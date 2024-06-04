using Engine.Graphics;
using Engine.Utils;
using System.Numerics;
using static Engine.HengineEcs;

namespace Runner
{
	static class TestWorld
	{
		public static void Load(Main world)
		{
			//var meshBox = Mesh.LoadGltf("Box", "Models/Box/box.gltf");
			//var meshDuck = Mesh.LoadGltf("Duck", "Models/Duck/Duck.gltf", true);
			//var materialDuck = PbrMaterial.LoadGltf("Duck", "Models/Duck/Duck.gltf");

			//var meshMap = GetMapMesh();

			//var meshBall = Mesh.LoadOBJ("Ball", "Models/Ball.obj");
			//var meshSphere = Mesh.LoadOBJ("Sphere", "Models/SphereSmooth.obj");

			var meshPlane = Mesh.LoadGltf("Plane", "Models/Plane/plane.gltf");

			var meshSphere = Mesh.LoadGltf("Sphere", "Models/Sphere/sphere.gltf");
			//meshSphere.RecalculateNormals();
			var materialSphere = GetMaterial();
			//var meshBox = Mesh.LoadOBJ("Box", "Models/Box.obj");

			//var texture = ETexture.LoadImage("Haakon", "Images/image_2.png");
			//var texture2 = ETexture.LoadImage("Statue", "Images/image.png");

			//world.CreateObject(new(3, 0, -10), Vector3.One, meshDuck, materialDuck, 1);
			//world.CreateObject(new(3, 0, 10), meshMap, materialDuck, 1);

			world.CreateObject(new(0, 3, -6), Vector3.One * 0.25f, meshSphere, materialSphere, 0);
			world.CreateObject(new(10, 0, -2), Vector3.One * 0.25f, meshSphere, materialSphere, 0);
			world.CreateObject(new(-10, 0, -2), Vector3.One * 0.25f, meshSphere, materialSphere, 0);
			world.CreateObject(new(0, 10, -2), Vector3.One * 0.25f, meshSphere, materialSphere, 0);
			/*
			*/

			//world.CreateObject(new(0, 0, 0), Vector3.One * 1.25f, meshSphere, materialSphere, 0);

			Vector3 s = new Vector3(0, 0, 0);
			Vector3 sd = new Vector3(2f, 1, 0);

			Vector3 e = new Vector3(1, 0, 0);
			Vector3 ed = new Vector3(-1f, 1, 0);

			for (int i = 0; i < 100; i++)
			{
				Vector3 c1 = Bezier.CubicBezierCurve(s, sd, ed, e, i / 100f);
				Vector3 c2 = Bezier.CubicBezierCurve(s, sd, ed, e, (i + 1) / 100f);

				world.CreateGizmoLine(c1, c2, new(0, 0, 0));
			}

			world.CreateGizmo(s, Vector3.One * 0.5f, Engine.Components.GizmoType.Point, new(0.005f, 0.005f, 0.005f));
			world.CreateGizmo(sd, Vector3.One * 0.5f, Engine.Components.GizmoType.Point, new(0.005f, 0.005f, 0.005f));
			world.CreateGizmo(e, Vector3.One * 0.5f, Engine.Components.GizmoType.Point, new(0.005f, 0.005f, 0.005f));
			world.CreateGizmo(ed, Vector3.One * 0.5f, Engine.Components.GizmoType.Point, new(0.005f, 0.005f, 0.005f));

			world.CreateObject(new(0, -4, 0), Vector3.One * 4, meshPlane, materialSphere, 0);

			float midX = 4 / 2f;
			float midY = 4f / 2f;

			for (int y = 0; y < 4; y++)
			{
				for (int x = 0; x < 4; x++)
				{
					world.CreateObject(new(-midX + x * 2.1f, -midY + y * 2.1f, -5), Vector3.One, meshSphere, materialSphere, 0);
				}
			}

			//Create(mainWorld, new(0, 0, -5), meshBall, texture, 0);
		}

		static PbrMaterial GetMaterial()
		{
			//var textureAlbedo = ETexture.LoadImage("PbrGoldAlbedo", "Images/Pbr/Gold/gold-scuffed_basecolor-boosted.png");
			//var textureAlbedo = ETexture.LoadImage("PbrGoldAlbedo", "Images/Pbr/Iron/rustediron2_basecolor.png");
			var textureAlbedo = ETexture.LoadImage("PbrGoldAlbedo", "Images/Pbr/Floor/wood_floor_worn_diff_4k.png");

			//var textureNormal = ETexture.LoadImage("PbrGoldNormal", "Images/Pbr/Gold/gold-scuffed_normal.png");
			//var textureNormal = ETexture.LoadImage("PbrGoldNormal", "Images/Pbr/Iron/rustediron2_normal.png");
			var textureNormal = ETexture.LoadImage("PbrGoldNormal", "Images/Pbr/Floor/wood_floor_worn_nor_gl_4k.png");
			//var textureNormal = ETexture.LoadImage("PbrGoldNormal", "Images/Pbr/Default/Normal.png");

			//var textureMetallic = ETexture.LoadImage("PbrGoldMetallic", "Images/Pbr/Gold/gold-scuffed_metallic.png");
			//var textureMetallic = ETexture.LoadImage("PbrGoldMetallic", "Images/Pbr/Iron/rustediron2_metallic.png");
			var textureMetallic = ETexture.LoadImage("PbrGoldMetallic", "Images/Pbr/Floor/wood_floor_worn_arm_4k.png");

			//var textureRoughness = ETexture.LoadImage("PbrGoldRoughness", "Images/Pbr/Gold/gold-scuffed_roughness.png");
			//var textureRoughness = ETexture.LoadImage("PbrGoldRoughness", "Images/Pbr/Iron/rustediron2_roughness.png");
			var textureRoughness = ETexture.LoadImage("PbrGoldRoughness", "Images/Pbr/Floor/wood_floor_worn_rough_4k.png");

			//var textureDepth = ETexture.LoadImage("PbrGoldDepth", "Images/Pbr/Floor/wood_floor_worn_disp_4k_new.png");

			//var textureAo = ETexture.LoadImage("PbrGoldAo", "Images/Pbr/Default/Ao.png");

			PbrMaterial material = PbrMaterial.GetDefault("PbrGold");
			material.albedo = Vector3.One;
			material.albedoMap = textureAlbedo;
			material.metallic = 1;
			material.metallicMap = textureMetallic;
			material.roughness = 8;
			material.roughnessMap = textureRoughness;
			//material.aoMap = textureAo;
			material.normalMap = textureNormal;
			//material.depthMap = textureDepth;

			return material;
		}
	}
}