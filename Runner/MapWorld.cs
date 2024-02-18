using Engine.Graphics;
using System.Numerics;
using static Engine.HengineEcs;

namespace Runner
{
	static class MapWorld
	{
		public static void Load(Main world)
		{
			var meshMap = GetMapMesh();
			var materialMap = GetMaterial();

			world.CreateObject(new(0, -2, -5), meshMap, materialMap, 1);
		}

		static PbrMaterial GetMaterial()
		{
			//var textureAlbedo = ETexture.LoadImage("PbrGoldAlbedo", "Images/Pbr/Gold/gold-scuffed_basecolor-boosted.png");
			//var textureAlbedo = ETexture.LoadImage("PbrGoldAlbedo", "Images/Pbr/Iron/rustediron2_basecolor.png");
			var textureAlbedo = ETexture.LoadImage("PbrGoldAlbedo", "Images/Pbr/Floor/wood_floor_worn_diff_4k.png");

			//var textureNormal = ETexture.LoadImage("PbrGoldNormal", "Images/Pbr/Gold/gold-scuffed_normal.png");
			//var textureNormal = ETexture.LoadImage("PbrGoldNormal", "Images/Pbr/Iron/rustediron2_normal.png");
			var textureNormal = ETexture.LoadImage("PbrGoldNormal", "Images/Pbr/Floor/wood_floor_worn_nor_gl_4k.png");

			//var textureMetallic = ETexture.LoadImage("PbrGoldMetallic", "Images/Pbr/Gold/gold-scuffed_metallic.png");
			//var textureMetallic = ETexture.LoadImage("PbrGoldMetallic", "Images/Pbr/Iron/rustediron2_metallic.png");
			var textureMetallic = ETexture.LoadImage("PbrGoldMetallic", "Images/Pbr/Floor/wood_floor_worn_arm_4k.png");

			//var textureRoughness = ETexture.LoadImage("PbrGoldRoughness", "Images/Pbr/Gold/gold-scuffed_roughness.png");
			//var textureRoughness = ETexture.LoadImage("PbrGoldRoughness", "Images/Pbr/Iron/rustediron2_roughness.png");
			var textureRoughness = ETexture.LoadImage("PbrGoldRoughness", "Images/Pbr/Floor/wood_floor_worn_rough_4k.png");

			var textureAo = ETexture.LoadImage("PbrGoldAo", "Images/Pbr/Default/Ao.png");

			PbrMaterial material = new PbrMaterial();
			material.name = "PbrGold";
			material.albedo = Vector3.One;
			material.albedoMap = textureAlbedo;
			material.metallic = 1;
			material.metallicMap = textureMetallic;
			material.roughness = 1;
			material.roughnessMap = textureRoughness;
			material.aoMap = textureAo;
			material.normalMap = textureNormal;

			return material;
		}

		static Mesh GetMapMesh()
		{
			const float OUTER_RADIUS = 1f;
			const float INNER_RADIUS = OUTER_RADIUS * 0.866025404f;

			var mesh = new Mesh();
			Vertex[] vericies = new Vertex[6];
			vericies[0] = new Vertex(new(0, 0, OUTER_RADIUS), Vector3.UnitY, new(0.5f, 0), Vector3.UnitX);
			vericies[1] = new Vertex(new(INNER_RADIUS, 0, OUTER_RADIUS * 0.5f), Vector3.UnitY, new(1, 0.33f), Vector3.UnitX);
			vericies[2] = new Vertex(new(INNER_RADIUS, 0, -OUTER_RADIUS * 0.5f), Vector3.UnitY, new(1, 0.66f), Vector3.UnitX);
			vericies[3] = new Vertex(new(0, 0, -OUTER_RADIUS), Vector3.UnitY, new(0.5f, 1), Vector3.UnitX);
			vericies[4] = new Vertex(new(-INNER_RADIUS, 0, -OUTER_RADIUS * 0.5f), Vector3.UnitY, new(0, 0.66f), Vector3.UnitX);
			vericies[5] = new Vertex(new(-INNER_RADIUS, 0, OUTER_RADIUS * 0.5f), Vector3.UnitY, new(0, 0.33f), Vector3.UnitX);

			uint[] indicies = new uint[3 * 4];
			indicies[0] = 2;
			indicies[1] = 3;
			indicies[2] = 1;

			indicies[3] = 3;
			indicies[4] = 4;
			indicies[5] = 0;

			indicies[6] = 1;
			indicies[7] = 3;
			indicies[8] = 0;

			indicies[9] = 4;
			indicies[10] = 5;
			indicies[11] = 0;

			mesh.name = "HexMap";
			//mesh.verticies = vericies;
			mesh.verticies = GetMapVerticies(2, 1);
			//mesh.indicies = indicies;
			mesh.indicies = GetMapIndicies(2, 1);

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