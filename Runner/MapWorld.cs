using CommunityToolkit.HighPerformance;
using EnCS;
using Engine;
using Engine.Components;
using Engine.Graphics;
using Engine.Utils;
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
			var materialMap = GetMapMaterial();

			HexMap map = new HexMap(4 * 4, 4 * 4);
			map.Init();
			var mapMesh = map.Compile();

			Span<Vertex> verticies = stackalloc Vertex[128];
			Span<uint> indicies = stackalloc uint[12];
			var meshBuilder = new MeshBuilder<Vertex, uint>(verticies, indicies);

			meshBuilder.AppendTriange(0, 1, 2);
			meshBuilder.AppendTriange(0, 2, 5);
			meshBuilder.AppendTriange(2, 3, 5);
			meshBuilder.AppendTriange(3, 4, 5);

			//CreateCenterVertex(verticies, Vector3.Zero, Vector2.Zero);
			HexMapBuilder.CreateHexVerticies(ref meshBuilder, Vector3.Zero, Vector3.One * 0.5f, Vector2.Zero);
			//HexMapBuilder.CreateOuterHexVerticies(ref meshBuilder, Vector3.Zero, Vector3.One * 0.75f, Vector3.One * 0f, Vector2.Zero);
			//HexMapBuilder.CreateOuterHexVerticies(ref meshBuilder, Vector3.Zero, Vector3.One * 0.75f, Vector3.One * 0.25f, Vector2.Zero);
			//CreateOuterHexVerticies(verticies.AsSpan().Slice(12 + 12), Vector3.Zero, Vector3.One * 0.5f, Vector3.One * 0.25f, Vector2.Zero, offsets);
			//CreateCenterVertex(verticies.AsSpan().Slice(7 + 24), Vector3.Zero, Vector2.Zero);
			//CreateHexVerticies(verticies.AsSpan().Slice(7 + 24 + 1), Vector3.Zero, Vector3.One, Vector2.Zero, offsets);
			int vertexLength = meshBuilder.VertexOffset;
			for (int i = 0; i < vertexLength; i++)
			{
				world.CreateGizmo(verticies[i].position, Vector3.One * 0.25f, GizmoType.Point, new GizmoColor(i / (float)vertexLength));
				//world.CreateGizmoLine(verticies[i].position, verticies[(i + 1) % vertexLength].position, new GizmoColor(0.0f));
			}

			for (int i = 0; i < meshBuilder.IndexOffset; i+=3)
			{
				world.CreateGizmoLine(verticies[(int)indicies[i]].position, verticies[(int)indicies[i + 1]].position, new GizmoColor(0.0f));
				world.CreateGizmoLine(verticies[(int)indicies[i + 1]].position, verticies[(int)indicies[i + 2]].position, new GizmoColor(0.0f));
				world.CreateGizmoLine(verticies[(int)indicies[i + 2]].position, verticies[(int)indicies[i]].position, new GizmoColor(0.0f));
			}

			float l1 = Random.Shared.NextSingle() * 6;
			float l2 = Random.Shared.NextSingle() * 6;

			//float l1 = 1.25f;
			//float l2 = 2.5f;

            Console.WriteLine($"L1: {l1}, L2: {l2}");

            Vector3 zero = Vector3.Zero;
			Vector3 p1 = CircularGetElement(verticies.Slice(0, vertexLength), l1);
			//world.CreateGizmo(p1, Vector3.One * 0.25f, GizmoType.Point, new GizmoColor(0.8f));

			Vector3 p2 = CircularGetElement(verticies.Slice(0, vertexLength), l2);
			//world.CreateGizmo(p2, Vector3.One * 0.25f, GizmoType.Point, new GizmoColor(0.8f));

			//world.CreateGizmoLine(verticies[5].position, verticies[0].position, new GizmoColor(0.0f));

			Vector3 start = new((-21 / 2) + 0.5f, -2, -5);
			start = new Vector3(0, 0, -5);
			world.CreateHex(start, Vector3.One, new HexCell(0), mapMesh, materialMap, 1);

			Span<Vector2> line = stackalloc Vector2[101];
			for (int i = 0; i < 100; i++)
			{
				Vector3 c1 = Bezier.CubicBezierCurve(p1, zero, zero, p2, i / 100f);
				Vector3 c2 = Bezier.CubicBezierCurve(p1, zero, zero, p2, (i + 1) / 100f);

				world.CreateGizmoLine(c1, c2, new(0, 0, 0));
				line[i] = new(c1.X, c1.Z);
            }

			Vector3 c11 = Bezier.CubicBezierCurve(p1, zero, zero, p2, 1);
			line[100] = new(c11.X, c11.Z);

			Span<Vector2> verticies2 = stackalloc Vector2[meshBuilder.VertexOffset];
			for (int i = 0; i < meshBuilder.VertexOffset; i++)
			{
				var pos = meshBuilder.verticies[i].position;
				verticies2[i] = new(pos.X, pos.Z);
			}

			Span<int> indicies2 = stackalloc int[meshBuilder.IndexOffset];
			for (int i = 0; i < meshBuilder.IndexOffset; i++)
			{
				indicies2[i] = (int)meshBuilder.indicies[i];
			}

			//Slicing.Slice([new Vector2(p1.X, p1.Z), new Vector2(p2.X, p2.Z)], verticies2, indicies2, out var newVerts, out var newIndicies);
			Slicing.Slice(line, verticies2, indicies2, out var newVerts, out var newIndicies);
			for (int i = 0; i < newVerts.Length; i++)
			{
				Vector2 point = newVerts.Span[i];
                world.CreateGizmo(new(point.X, 0, point.Y), Vector3.One * 0.04f, GizmoType.Point, new GizmoColor(0, 1, 0));
			}
		}

		public static Vector3 CircularGetElement(scoped Span<Vertex> buff, float lerp)
		{
			float integral = MathF.Truncate(lerp);
			float fractional = lerp - integral;

			int i1 = (int)(lerp % buff.Length);
			int i2 = (int)((lerp + 1) % buff.Length);

			return Vector3.Lerp(buff[i1].position, buff[i2].position, fractional);
		}

		public static Vector3 RandomColor()
		{
			return new Vector3(Random.Shared.NextSingle(), Random.Shared.NextSingle(), Random.Shared.NextSingle());
		}

		static PbrMaterial mMaterial = PbrMaterial.GetDefault($"Point_2");

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
	}
}