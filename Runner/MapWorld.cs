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

			meshBuilder.AppendTriange(0, 2, 5);
			meshBuilder.AppendTriange(0, 1, 2);
			meshBuilder.AppendTriange(2, 3, 5);
			meshBuilder.AppendTriange(3, 4, 5);
			/*
			*/

			//CreateCenterVertex(verticies, Vector3f.Zero, Vector2.Zero);
			HexMapBuilder.CreateHexVerticies(ref meshBuilder, Vector3f.Zero, Vector3f.One * 0.5f, Vector2f.Zero);
			//HexMapBuilder.CreateOuterHexVerticies(ref meshBuilder, Vector3f.Zero, Vector3f.One * 0.75f, Vector3f.One * 0f, Vector2.Zero);
			//HexMapBuilder.CreateOuterHexVerticies(ref meshBuilder, Vector3f.Zero, Vector3f.One * 0.75f, Vector3f.One * 0.25f, Vector2.Zero);
			//CreateOuterHexVerticies(verticies.AsSpan().Slice(12 + 12), Vector3f.Zero, Vector3f.One * 0.5f, Vector3f.One * 0.25f, Vector2.Zero, offsets);
			//CreateCenterVertex(verticies.AsSpan().Slice(7 + 24), Vector3f.Zero, Vector2.Zero);
			//CreateHexVerticies(verticies.AsSpan().Slice(7 + 24 + 1), Vector3f.Zero, Vector3f.One, Vector2.Zero, offsets);
			int vertexLength = meshBuilder.VertexOffset;
			for (int i = 0; i < vertexLength; i++)
			{
				world.CreateGizmo(verticies[i].position, Vector3f.One * 0.25f, GizmoType.Point, new GizmoColor(i / (float)vertexLength));
				//world.CreateGizmoLine(verticies[i].position, verticies[(i + 1) % vertexLength].position, new GizmoColor(0.0f));
			}

			for (int i = 0; i < meshBuilder.IndexOffset; i+=3)
			{
				//world.CreateGizmoLine(verticies[(int)indicies[i]].position, verticies[(int)indicies[i + 1]].position, new GizmoColor(0.0f));
				//world.CreateGizmoLine(verticies[(int)indicies[i + 1]].position, verticies[(int)indicies[i + 2]].position, new GizmoColor(0.0f));
				//world.CreateGizmoLine(verticies[(int)indicies[i + 2]].position, verticies[(int)indicies[i]].position, new GizmoColor(0.0f));
			}

			float l1 = Random.Shared.NextSingle() * 6;
			float l2 = Random.Shared.NextSingle() * 6;

			//float l1 = 1.25f;
			//float l2 = 0.5f;

			Console.WriteLine($"L1: {l1}, L2: {l2}");

            Vector3f zero = Vector3f.Zero;
			Vector3f p1 = CircularGetElement(verticies.Slice(0, vertexLength), l1);
			//world.CreateGizmo(p1, Vector3f.One * 0.25f, GizmoType.Point, new GizmoColor(0.8f));

			Vector3f p2 = CircularGetElement(verticies.Slice(0, vertexLength), l2);
			//world.CreateGizmo(p2, Vector3f.One * 0.25f, GizmoType.Point, new GizmoColor(0.8f));

			//world.CreateGizmoLine(verticies[5].position, verticies[0].position, new GizmoColor(0.0f));

			Vector3f start = new((-21 / 2) + 0.5f, -2, -5);
			start = new Vector3f(0, 0, -5);
			world.CreateHex(start, Vector3f.One, new HexCell(0), mapMesh, materialMap, 1);

			int lineRes = 5;
			Span<Vector2f> line = stackalloc Vector2f[lineRes + 1];
			for (int i = 0; i < lineRes; i++)
			{
                //Console.WriteLine($"Line: {i} - {(i / (float)lineRes)}");
                Vector3f c1 = Bezier.CubicBezierCurve(p1, zero, zero, p2, i / (float)lineRes);
				Vector3f c2 = Bezier.CubicBezierCurve(p1, zero, zero, p2, (i + 1) / (float)lineRes);

				//if (i == 7)
				//world.CreateGizmoLine(c1, c2, new(0, 0, 0));

				line[i] = new(c1.x, c1.z);
            }

			Vector3f c11 = Bezier.CubicBezierCurve(p1, zero, zero, p2, 1);
			line[lineRes] = new(c11.x, c11.z);

			Span<Vector2f> verticies2 = stackalloc Vector2f[meshBuilder.VertexOffset];
			for (int i = 0; i < meshBuilder.VertexOffset; i++)
			{
				var pos = meshBuilder.verticies[i].position;
				verticies2[i] = new(pos.x, pos.z);
			}

			Span<int> indicies2 = stackalloc int[meshBuilder.IndexOffset];
			for (int i = 0; i < meshBuilder.IndexOffset; i++)
			{
				indicies2[i] = (int)meshBuilder.indicies[i];
			}

			static Vector3f ToVec3(in Vector2f v)
			{
				return new(v.x, 0, v.y);
			}

			Slicing.Slice(line, verticies2, indicies2, out var newVerts, out var newIndicies, out var seam, margin: 0.001f);
			Splitting.Split(newVerts.Span, newIndicies.Span, seam.Span, out var vr, out var ir, out var vl, out var il);

			for (int i = 0; i < seam.Length; i++)
			{
				//newVerts.Span[seam.Span[i]] += new Vector2(0, 1f);
			}

			for (int i = 0; i < newVerts.Length; i++)
			{
				Vector2f point = newVerts.Span[i];
                world.CreateGizmo(new(point.x, 0, point.y), Vector3f.One * 0.04f, GizmoType.Point, new GizmoColor(0, 1, 0));
			}

			for (int i = 0; i < newIndicies.Length; i += 3)
			{
                world.CreateGizmoLine(ToVec3(newVerts.Span[newIndicies.Span[i]])    , ToVec3(newVerts.Span[newIndicies.Span[i + 1]]), new GizmoColor(0.0f));
				world.CreateGizmoLine(ToVec3(newVerts.Span[newIndicies.Span[i + 1]]), ToVec3(newVerts.Span[newIndicies.Span[i + 2]]), new GizmoColor(0.0f));
				world.CreateGizmoLine(ToVec3(newVerts.Span[newIndicies.Span[i + 2]]), ToVec3(newVerts.Span[newIndicies.Span[i]]), new GizmoColor(0.0f));
			}
			/*
			*/

			//Vector2 point = newVerts.Span[5];
			//world.CreateGizmo(new(point.X, 0, point.Y), Vector3f.One * 0.04f, GizmoType.Point, new GizmoColor(0, 1, 0));

			Engine.Utils.Mesh.MeshBuilder.Build(x =>
			{
				ref readonly var p = ref x.Plane(Vector3f.Zero, Vector3f.UnitY, x =>
				{
					ref readonly var hex = ref x.Square(Vector2.Zero, Vector2.One);
				});
			}, out var verts, out var inds);
		}

		public static Vector3f CircularGetElement(scoped Span<Vertex> buff, float lerp)
		{
			float integral = MathF.Truncate(lerp);
			float fractional = lerp - integral;

			int i1 = (int)(lerp % buff.Length);
			int i2 = (int)((lerp + 1) % buff.Length);

			return Vector3f.Lerp(buff[i1].position, buff[i2].position, fractional);
		}

		public static Vector3f RandomColor()
		{
			return new Vector3f(Random.Shared.NextSingle(), Random.Shared.NextSingle(), Random.Shared.NextSingle());
		}

		static PbrMaterial mMaterial = PbrMaterial.GetDefault($"Point_2");

		static PbrMaterial GetMapMaterial()
		{
			PbrMaterial material = mMaterial;
			material.name = $"Map";
			material.albedo = Vector3f.One;
			material.albedoMap = ETexture.LoadImage($"Map_albedo", "Images/AlbedoMap.png");

			return material;
		}

		static PbrMaterial pMaterial = PbrMaterial.GetDefault($"Point");

		public static PbrMaterial GetPointMaterial(Vector3f color)
		{
			PbrMaterial material = pMaterial;
			material.name = $"Point: {color}";
			material.albedo = color;


			return material;
		}
	}
}