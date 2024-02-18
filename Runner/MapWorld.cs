using EnCS;
using Engine.Graphics;
using Silk.NET.Vulkan;
using System;
using System.Numerics;
using static Engine.HengineEcs;

namespace Runner
{
	static class MapWorld
	{
		public static void Load(Main world)
		{
			HexMap map = new HexMap(21, 21);

			var meshSphere = Mesh.LoadOBJ("Sphere", "Models/SphereSmooth.obj");
			var materialSphere = GetPointMaterial(Vector3.Zero);

			var meshMap = GetMapMesh();
			var materialMap = GetMapMaterial();

			map.Init(world, ref meshSphere, ref meshMap, ref materialSphere);

			using var neighbors = map.GetNeighbors(new HexCoord(5, 5));

            var blackMat = GetPointMaterial(Vector3.Zero);
			for (int i = 0; i < 6; i++)
			{
				world.Get(neighbors.Memory.Span[i]).PbrMaterial.Set(blackMat);
			}
		}

		public static Vector3 RandomColor()
		{
			return new Vector3(Random.Shared.NextSingle(), Random.Shared.NextSingle(), Random.Shared.NextSingle());
		}

		static PbrMaterial GetMapMaterial()
		{
			PbrMaterial material = PbrMaterial.GetDefault("Map");

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
			const float OUTER_RADIUS = 1f;
			const float INNER_RADIUS = OUTER_RADIUS * 0.866025404f;

			var mesh = new Mesh();
			Vertex[] vericies = new Vertex[7];
			vericies[0] = new Vertex(new(0, 0, 0), Vector3.UnitY, new(0.5f, 0), Vector3.UnitX);
			vericies[1] = new Vertex(new(0, 0, OUTER_RADIUS), Vector3.UnitY, new(0.5f, 0), Vector3.UnitX);
			vericies[2] = new Vertex(new(INNER_RADIUS, 0, OUTER_RADIUS * 0.5f), Vector3.UnitY, new(1, 0.33f), Vector3.UnitX);
			vericies[3] = new Vertex(new(INNER_RADIUS, 0, -OUTER_RADIUS * 0.5f), Vector3.UnitY, new(1, 0.66f), Vector3.UnitX);
			vericies[4] = new Vertex(new(0, 0, -OUTER_RADIUS), Vector3.UnitY, new(0.5f, 1), Vector3.UnitX);
			vericies[5] = new Vertex(new(-INNER_RADIUS, 0, -OUTER_RADIUS * 0.5f), Vector3.UnitY, new(0, 0.66f), Vector3.UnitX);
			vericies[6] = new Vertex(new(-INNER_RADIUS, 0, OUTER_RADIUS * 0.5f), Vector3.UnitY, new(0, 0.33f), Vector3.UnitX);

			uint[] indicies = new uint[3 * 6];
			indicies[0] = 0;
			indicies[1] = 1;
			indicies[2] = 2;
			
			indicies[3] = 0;
			indicies[4] = 2;
			indicies[5] = 3;
			
			indicies[6] = 0;
			indicies[7] = 3;
			indicies[8] = 4;
			
			indicies[9] = 0;
			indicies[10] = 4;
			indicies[11] = 5;
			
			indicies[12] = 0;
			indicies[13] = 5;
			indicies[14] = 6;
			
			indicies[15] = 0;
			indicies[16] = 6;
			indicies[17] = 1;

			mesh.name = "HexMap";
			mesh.verticies = vericies;
			//mesh.verticies = GetMapVerticies(2, 1);
			mesh.indicies = indicies;
			//mesh.indicies = GetMapIndicies(2, 1);

			return mesh;
		}

		static Vertex[] GetMapVerticies(int width, int height)
		{
			const float OUTER_RADIUS = 1f;
			const float INNER_RADIUS = OUTER_RADIUS * 0.866025404f;

			List<Vertex> verticeis = new();

			for (int y = 0; y < height; y++)
			{
				float posY = y * OUTER_RADIUS;
				float posX = y * INNER_RADIUS;

				verticeis.Add(new(new(posX + -INNER_RADIUS, 0, posY + -OUTER_RADIUS * 0.5f), Vector3.UnitY, new(), Vector3.UnitX));
				verticeis.Add(new(new(posX + -INNER_RADIUS, 0, posY + OUTER_RADIUS * 0.5f), Vector3.UnitY, new(), Vector3.UnitX));

				for (int x = 0; x < width; x++)
				{
					posX = x * INNER_RADIUS * 2 + y * INNER_RADIUS;

					if (y == height - 1)
						verticeis.Add(new(new(posX, 0, posY + OUTER_RADIUS), Vector3.UnitY, new(Random.Shared.NextSingle()), Vector3.UnitX));

					if (y == 0)
						verticeis.Add(new(new(posX, 0, posY + -OUTER_RADIUS), Vector3.UnitY, new(Random.Shared.NextSingle()), Vector3.UnitX));

					verticeis.Add(new(new(posX + INNER_RADIUS, 0, posY + -OUTER_RADIUS * 0.5f), Vector3.UnitY, new(Random.Shared.NextSingle()), Vector3.UnitX));
					verticeis.Add(new(new(posX + INNER_RADIUS, 0, posY + OUTER_RADIUS * 0.5f), Vector3.UnitY, new(Random.Shared.NextSingle()), Vector3.UnitX));
				}
			}

			return verticeis.ToArray();
		}

		static uint[] GetMapIndicies(int width, int height)
		{
			List<uint> indicies = new();

			for (uint x = 0; x < width; x++)
			{
				uint offset = x * 4;

				indicies.Add(offset + 2);
				indicies.Add(offset + 5);
				indicies.Add(offset + 1);

				indicies.Add(offset + 5);
				indicies.Add(offset + 4);
				indicies.Add(offset + 0);

				indicies.Add(offset + 1);
				indicies.Add(offset + 5);
				indicies.Add(offset + 0);

				indicies.Add(offset + 4);
				indicies.Add(offset + 3);
				indicies.Add(offset + 0);
			}

			return indicies.ToArray();
		}
	}
}