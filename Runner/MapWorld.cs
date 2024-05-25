using EnCS;
using Engine;
using Engine.Components;
using Engine.Graphics;
using Silk.NET.Vulkan;
using System;
using System.Numerics;
using System.Xml.Linq;
using static Engine.HengineEcs;

namespace Runner
{
	static class MapWorld
	{
		const float OUTER_RADIUS = 1f;
		const float INNER_RADIUS = OUTER_RADIUS * 0.866025404f;

		public static void Load(Main world)
		{
			HexMap map = new HexMap(4 * 8, 4 * 8);

			var meshSphere = Mesh.LoadOBJ("Sphere", "Models/SphereSmooth.obj");
			var materialSphere = GetPointMaterial(Vector3.Zero);

			var meshMap = GetMapMesh();
			var materialMap = GetMapMaterial();

			map.Init();
			var mapMesh = map.Compile();

			//using var neighbors = map.GetNeighbors(new HexCoord(5, 5));

			Span<Vector3> offsets = stackalloc Vector3[6];

			Vertex[] verticies = new Vertex[128];
			//CreateCenterVertex(verticies, Vector3.Zero, Vector2.Zero);
			//CreateHexVerticies(verticies.AsSpan().Slice(1), Vector3.Zero, Vector3.One * 0.5f, Vector2.Zero, offsets);
			CreateOuterHexVerticies(verticies.AsSpan().Slice(0), Vector3.Zero, Vector3.One * 0.5f, Vector3.One * 0f, Vector2.Zero, offsets);
			CreateOuterHexVerticies(verticies.AsSpan().Slice(12), Vector3.Zero, Vector3.One * 0.5f, Vector3.One * 0.25f, Vector2.Zero, offsets);
			CreateOuterHexVerticies(verticies.AsSpan().Slice(12 + 12), Vector3.Zero, Vector3.One * 0.5f, Vector3.One * 0.5f, Vector2.Zero, offsets);
			//CreateCenterVertex(verticies.AsSpan().Slice(7 + 24), Vector3.Zero, Vector2.Zero);
			//CreateHexVerticies(verticies.AsSpan().Slice(7 + 24 + 1), Vector3.Zero, Vector3.One, Vector2.Zero, offsets);
			for (int i = 0; i < verticies.Length; i++)
			{
				world.CreateObject(verticies[i].position * 0.5f, Vector3.One * 0.01f, meshSphere, materialSphere, 0);
			}

            //Console.WriteLine(mapMesh.verticies.Length);

            Vector3 start = new((-21 / 2) + 0.5f, -2, -5);
			start = Vector3.Zero;
			world.CreateHex(start, Vector3.One, new HexCell(0), mapMesh, materialMap, 1);
		}

		public static Vector3 RandomColor()
		{
			return new Vector3(Random.Shared.NextSingle(), Random.Shared.NextSingle(), Random.Shared.NextSingle());
		}

		static PbrMaterial mMaterial = PbrMaterial.GetDefault($"Point");

		static PbrMaterial GetMapMaterial()
		{
			PbrMaterial material = mMaterial;
			material.name = $"Map";
			material.albedo = Vector3.One;
			material.albedoMap = ETexture.LoadImage($"Map_albedo", "Images/AlbedoMap.png");

			return material;
		}

		static PbrMaterial pMaterial = PbrMaterial.GetDefault($"Point");

		public static PbrMaterial GetPointMaterial(Vector3 color)
		{
			PbrMaterial material = pMaterial;
			material.name = $"Point: {color}";
			material.albedo = color;


			return material;
		}

		static Mesh GetMapMesh()
		{
			Span<Vector3> offsets = stackalloc Vector3[6];

			var mesh = new Mesh();
			Vertex[] vericies = new Vertex[7];
			CreateHexVerticies(vericies, Vector3.Zero, Vector3.One, Vector2.Zero, offsets);

			uint[] indicies = new uint[3 * 6];
			CreateHexIndicies(indicies, 0);

			mesh.name = "HexMap";
			mesh.verticies = vericies;
			mesh.indicies = indicies;

			return mesh;
		}

		public static void CreateCenterVertex(Span<Vertex> verticies, Vector3 pos, Vector2 uv)
		{
			verticies[0] = new Vertex(new Vector3(0, 0, 0) + pos, Vector3.UnitY, uv, Vector3.UnitX);
		}

		public static void CreateHexVerticies(Span<Vertex> verticies, Vector3 pos, Vector3 scale, Vector2 uv, Span<Vector3> neighborOffset)
		{
			/*
			verticies[0] = new Vertex(new Vector3(0, 0, 0) * scale + pos								, Vector3.UnitY, new(0.5f, 0)	, Vector3.UnitX);
			verticies[1] = new Vertex(new Vector3(0, 0, OUTER_RADIUS) * scale + pos						, Vector3.UnitY, new(0.5f, 0)	, Vector3.UnitX);
			verticies[2] = new Vertex(new Vector3(INNER_RADIUS, 0, OUTER_RADIUS * 0.5f) * scale + pos	, Vector3.UnitY, new(1, 0.33f)	, Vector3.UnitX);
			verticies[3] = new Vertex(new Vector3(INNER_RADIUS, 0, -OUTER_RADIUS * 0.5f) * scale + pos	, Vector3.UnitY, new(1, 0.66f)	, Vector3.UnitX);
			verticies[4] = new Vertex(new Vector3(0, 0, -OUTER_RADIUS) * scale + pos					, Vector3.UnitY, new(0.5f, 1)	, Vector3.UnitX);
			verticies[5] = new Vertex(new Vector3(-INNER_RADIUS, 0, -OUTER_RADIUS * 0.5f) * scale + pos	, Vector3.UnitY, new(0, 0.66f)	, Vector3.UnitX);
			verticies[6] = new Vertex(new Vector3(-INNER_RADIUS, 0, OUTER_RADIUS * 0.5f) * scale + pos	, Vector3.UnitY, new(0, 0.33f)	, Vector3.UnitX);
			*/

			Vector3 p0 = new Vector3(0, 0, OUTER_RADIUS);
			Vector3 p1 = new Vector3(INNER_RADIUS, 0, OUTER_RADIUS * 0.5f);
			Vector3 p2 = new Vector3(INNER_RADIUS, 0, -OUTER_RADIUS * 0.5f);
			Vector3 p3 = new Vector3(0, 0, -OUTER_RADIUS);
			Vector3 p4 = new Vector3(-INNER_RADIUS, 0, -OUTER_RADIUS * 0.5f);
			Vector3 p5 = new Vector3(-INNER_RADIUS, 0, OUTER_RADIUS * 0.5f);

			p0 *= scale;
			p1 *= scale;
			p2 *= scale;
			p3 *= scale;
			p4 *= scale;
			p5 *= scale;

			var o0 = neighborOffset[0];
			var o1 = neighborOffset[1];
			var o2 = neighborOffset[2];
			var o3 = neighborOffset[3];
			var o4 = neighborOffset[4];
			var o5 = neighborOffset[5];

			//verticies[0] = new Vertex(new Vector3(0, 0, 0) * scale + pos								, Vector3.UnitY, uv	, Vector3.UnitX);
			verticies[0] = new Vertex(p0 + pos + o0, Vector3.UnitY, uv, Vector3.UnitX);
			verticies[1] = new Vertex(p1 + pos + o1, Vector3.UnitY, uv, Vector3.UnitX);
			verticies[2] = new Vertex(p2 + pos + o2, Vector3.UnitY, uv, Vector3.UnitX);
			verticies[3] = new Vertex(p3 + pos + o3, Vector3.UnitY, uv, Vector3.UnitX);
			verticies[4] = new Vertex(p4 + pos + o4, Vector3.UnitY, uv, Vector3.UnitX);
			verticies[5] = new Vertex(p5 + pos + o5, Vector3.UnitY, uv, Vector3.UnitX);
		}

		public static void CreateOuterHexVerticies(Span<Vertex> verticies, Vector3 pos, Vector3 scale, Vector3 bridgeScale, Vector2 uv, Span<Vector3> neighborOffset)
		{
			Vector3 p0 = new Vector3(0, 0, OUTER_RADIUS);
			Vector3 p1 = new Vector3(INNER_RADIUS, 0, OUTER_RADIUS * 0.5f);
			Vector3 p2 = new Vector3(INNER_RADIUS, 0, -OUTER_RADIUS * 0.5f);
			Vector3 p3 = new Vector3(0, 0, -OUTER_RADIUS);
			Vector3 p4 = new Vector3(-INNER_RADIUS, 0, -OUTER_RADIUS * 0.5f);
			Vector3 p5 = new Vector3(-INNER_RADIUS, 0, OUTER_RADIUS * 0.5f);

			Vector3 right0 = Vector3.Cross(p1 - p0, Vector3.UnitY);
			Vector3 right1 = Vector3.Cross(p2 - p1, Vector3.UnitY);
			Vector3 right2 = Vector3.Cross(p3 - p2, Vector3.UnitY);
			Vector3 right3 = Vector3.Cross(p4 - p3, Vector3.UnitY);
			Vector3 right4 = Vector3.Cross(p5 - p4, Vector3.UnitY);
			Vector3 right5 = Vector3.Cross(p0 - p5, Vector3.UnitY);

			p0 *= scale;
			p1 *= scale;
			p2 *= scale;
			p3 *= scale;
			p4 *= scale;
			p5 *= scale;

			var o0 = neighborOffset[0];
			var o1 = neighborOffset[1];
			var o2 = neighborOffset[2];
			var o3 = neighborOffset[3];
			var o4 = neighborOffset[4];
			var o5 = neighborOffset[5];

			right0 = right0 * INNER_RADIUS * bridgeScale;
			right1 = right1 * INNER_RADIUS * bridgeScale;
			right2 = right2 * INNER_RADIUS * bridgeScale;
			right3 = right3 * INNER_RADIUS * bridgeScale;
			right4 = right4 * INNER_RADIUS * bridgeScale;
			right5 = right5 * INNER_RADIUS * bridgeScale;

			verticies[0] =	new Vertex(p0 + right0 + pos + o0, Vector3.UnitY, uv, Vector3.UnitX);
			verticies[1] =	new Vertex(p1 + right0 + pos + o0, Vector3.UnitY, uv, Vector3.UnitX);
			verticies[2] =	new Vertex(p1 + right1 + pos + o1, Vector3.UnitY, uv, Vector3.UnitX);
			verticies[3] =	new Vertex(p2 + right1 + pos + o1, Vector3.UnitY, uv, Vector3.UnitX);
			verticies[4] =	new Vertex(p2 + right2 + pos + o2, Vector3.UnitY, uv, Vector3.UnitX);
			verticies[5] =	new Vertex(p3 + right2 + pos + o2, Vector3.UnitY, uv, Vector3.UnitX);
			verticies[6] =	new Vertex(p3 + right3 + pos + o3, Vector3.UnitY, uv, Vector3.UnitX);
			verticies[7] =	new Vertex(p4 + right3 + pos + o3, Vector3.UnitY, uv, Vector3.UnitX);
			verticies[8] =	new Vertex(p4 + right4 + pos + o4, Vector3.UnitY, uv, Vector3.UnitX);
			verticies[9] =	new Vertex(p5 + right4 + pos + o4, Vector3.UnitY, uv, Vector3.UnitX);
			verticies[10] = new Vertex(p5 + right5 + pos + o5, Vector3.UnitY, uv, Vector3.UnitX);
			verticies[11] = new Vertex(p0 + right5 + pos + o5, Vector3.UnitY, uv, Vector3.UnitX);
		}

		public static void CreateHexIndicies(Span<uint> indicies, uint offset)
		{
			indicies[0] = offset;
			indicies[1] = offset + 1;
			indicies[2] = offset + 2;

			indicies[3] = offset;
			indicies[4] = offset + 2;
			indicies[5] = offset + 3;

			indicies[6] = offset;
			indicies[7] = offset + 3;
			indicies[8] = offset + 4;

			indicies[9] = offset;
			indicies[10] = offset + 4;
			indicies[11] = offset + 5;

			indicies[12] = offset;
			indicies[13] = offset + 5;
			indicies[14] = offset + 6;

			indicies[15] = offset;
			indicies[16] = offset + 6;
			indicies[17] = offset + 1;
		}

		static void CreateHexBridge(Span<uint> indicies, uint offset, uint bridgeOffset, uint maxBridgeOffset)
		{
			indicies[0] = offset + bridgeOffset * 2;
			indicies[1] = offset + 12 + bridgeOffset * 2;
			indicies[2] = offset + 12 + bridgeOffset * 2 + 1;

			indicies[3] = offset + bridgeOffset * 2;
			indicies[4] = offset + 12 + bridgeOffset * 2 + 1;
			indicies[5] = offset + bridgeOffset * 2 + 1;
		}

		static void CreateHexBridgeCorner(Span<uint> indicies, uint offset, uint bridgeOffset, uint maxBridgeOffset)
		{
			indicies[0] = offset + bridgeOffset;
			indicies[1] = offset + 6 + 12 + bridgeOffset;
			indicies[2] = offset + 6 + bridgeOffset * 2;

			indicies[3] = offset + bridgeOffset;
			indicies[4] = offset + 6 + ((maxBridgeOffset * 2 + 2 ) + bridgeOffset * 2 - 1) % (maxBridgeOffset * 2 +  2);
			indicies[5] = offset + 6 + 12 + bridgeOffset;
        }

		public static void CreateHexBridgeIndicies(Span<uint> indicies, uint offset)
		{
			CreateHexBridge(indicies[0..], offset, 0, 5);
			CreateHexBridge(indicies[6..], offset, 1, 5);
			CreateHexBridge(indicies[12..], offset, 2, 5);
			CreateHexBridge(indicies[18..], offset, 3, 5);
			CreateHexBridge(indicies[24..], offset, 4, 5);
			CreateHexBridge(indicies[30..], offset, 5, 5);
		}

		public static void CreateHexBridgeCorners(Span<uint> indicies, uint offset)
		{
			CreateHexBridgeCorner(indicies[0..], offset, 0, 5);
			CreateHexBridgeCorner(indicies[6..], offset, 1, 5);
			CreateHexBridgeCorner(indicies[12..], offset, 2, 5);
			CreateHexBridgeCorner(indicies[18..], offset, 3, 5);
			CreateHexBridgeCorner(indicies[24..], offset, 4, 5);
			CreateHexBridgeCorner(indicies[30..], offset, 5, 5);
		}
	}
}