using Engine;
using Engine.Components;
using Engine.Components.Graphics;
using Engine.Graphics;
using Engine.Parsing;
using Silk.NET.Maths;
using Silk.NET.OpenAL;
using Silk.NET.Windowing;
using System.Net;
using System.Numerics;
using static Engine.HengineEcs;

namespace Runner
{
	internal class Program
	{
		static void Create(Main world, Vector3 pos, Mesh mesh, ETexture texture, int idx)
		{
			var objRef = world.Create(new NEntity());
			NEntity.Ref entRef = world.Get(objRef);
			entRef.Position.Set(pos);
			entRef.Scale.Set(Vector3.One);
			entRef.Rotation.Set(Quaternion.Identity);
			entRef.Mesh.Set(mesh);
			entRef.ETexture.Set(texture);
			entRef.Networked.Set(new Networked()
			{
				idx = idx
			});
		}

        static void Create(Main world, Vector3 pos, Mesh mesh, PbrMaterialNew material, int idx)
        {
            var objRef = world.Create(new NEntity());
            NEntity.Ref entRef = world.Get(objRef);
            entRef.Position.Set(pos);
            entRef.Scale.Set(Vector3.One);
            entRef.Rotation.Set(Quaternion.Identity);
            entRef.Mesh.Set(mesh);
            entRef.PbrMaterialNew.Set(material);
            entRef.Networked.Set(new Networked()
            {
                idx = idx
            });
        }

        static void CreateCamera(Main world, Camera camera, Vector3 position)
		{
			var objRef = world.Create(new Cam());
			Cam.Ref entRef = world.Get(objRef);
			entRef.Camera.Set(camera);
			entRef.Position.Set(position);
			entRef.Rotation.Set(Quaternion.Identity);
			entRef.Networked.Set(new Networked());
		}

		static void CreateCamera(HengineServerEcs.Main world, Camera camera, Vector3 position, int idx = 0)
		{
			var objRef = world.Create(new HengineServerEcs.Cam());
			HengineServerEcs.Cam.Ref entRef = world.Get(objRef);
			entRef.Camera.Set(camera);
			entRef.Position.Set(position);
			entRef.Rotation.Set(Quaternion.Identity);
			entRef.Networked.Set(new Networked()
			{
				idx = idx
			});
		}

		static void Main(string[] args)
		{
			ImageFormatSetup.HdrSetup();

			var engineConfig = new EngineConfig()
			{
				appName = "Hengine v5",
				appVersion = new Version(0, 0, 1),

				engineName = "Hengine",
				engineVersion = new Version(5, 0, 0),

				idx = 0
			};

			if (args.Length > 0 && args[0] == "server")
			{
				Server(engineConfig);
			}
			else
			{
				if (args.Length > 1)
					engineConfig.idx = int.Parse(args[1]);

				Client(engineConfig);
			}
		}

		static void Client(EngineConfig engineConfig)
		{
			var vulkanConfig = new VulkanConfig()
			{
				validationLayers = ["VK_LAYER_KHRONOS_validation"]
			};

			var networkConfig = new NetworkConfig()
			{
				ipAddress = IPAddress.Parse("127.0.0.1"),
				port = 45567
			};

			Hengine engine = new Hengine();
			engine.Initialize(engineConfig, vulkanConfig, networkConfig);

			var ecs = engine.GetEcs();
			Main mainWorld = ecs.GetMain();

			//var meshBox = Mesh.LoadGltf("Box", "Models/Box/box.gltf");
			var meshDuck = Mesh.LoadGltf("Duck", "Models/Duck/Duck.gltf", true);
			var materialDuck = PbrMaterialNew.LoadGltf("Duck", "Models/Duck/Duck.gltf");
			//var meshBall = Mesh.LoadOBJ("Ball", "Models/Ball.obj");
			var meshSphere = Mesh.LoadOBJ("Sphere", "Models/SphereSmooth.obj");
			var materialSphere = GetMaterial();
			//var meshBox = Mesh.LoadOBJ("Box", "Models/Box.obj");

			//var texture = ETexture.LoadImage("Haakon", "Images/image_2.png");
			//var texture2 = ETexture.LoadImage("Statue", "Images/image.png");

			Camera camera = new Camera
			{
				width = 800,
				height = 600,
				fov = 1.22173f, // 70 degrees
				zNear = 0.1f,
				zFar = 1000
			};

			CreateCamera(mainWorld, camera, Vector3.Zero);

			Create(mainWorld, new(3, 0, -10), meshDuck, materialDuck, 1);
			/*
			Create(mainWorld, new(0, 0, -5), meshBall, texture, 0);
			Create(mainWorld, new(-3, 0, -5), meshSphere, texture2, 11);
			*/

			float midX = 4 / 2f;
			float midY = 4f / 2f;

			for (int y = 0; y < 4; y++)
			{
				for (int x = 0; x < 4; x++)
				{
					Create(mainWorld, new(-midX + x * 2.1f, -midY + y * 2.1f, -5), meshSphere, materialSphere, 0);
				}
			}

			//Create(mainWorld, new(0, 0, -5), meshBall, texture, 0);

			engine.Start();
			engine.argIWindow.Dispose();
		}

		static void Server(EngineConfig engineConfig)
		{
			var networkConfig = new NetworkConfig()
			{
				ipAddress = IPAddress.Parse("127.0.0.1"),
				port = 45567
			};

			HengineServer server = new HengineServer();
			server.Initialize(engineConfig, networkConfig);

			var ecs = server.GetEcs();
			HengineServerEcs.Main mainWorld = ecs.GetMain();

			Camera camera = new Camera
			{
				width = 800,
				height = 600,
				fov = 1.22173f, // 70 degrees
				zNear = 0.1f,
				zFar = 1000
			};

			CreateCamera(mainWorld, camera, Vector3.Zero, 0);
			CreateCamera(mainWorld, camera, Vector3.Zero, 1);

			server.Start();
		}

		static PbrMaterialNew GetMaterial()
		{
            var textureAlbedo = ETexture.LoadImage("PbrGoldAlbedo", "Images/Pbr/Gold/gold-scuffed_basecolor-boosted.png");
            //var textureAlbedo = ETexture.LoadImage("PbrGoldAlbedo", "Images/Pbr/Iron/rustediron2_basecolor.png");
            //var textureAlbedo = ETexture.LoadImage("PbrGoldAlbedo", "Images/Pbr/Floor/wood_floor_worn_diff_4k.png");

            //var textureNormal = ETexture.LoadImage("PbrGoldAlbedo", "Images/Pbr/Gold/gold-scuffed_normal.png");
            //var textureNormal = ETexture.LoadImage("PbrGoldAlbedo", "Images/Pbr/Iron/rustediron2_normal.png");
            //var textureNormal = ETexture.LoadImage("PbrGoldAlbedo", "Images/Pbr/Floor/wood_floor_worn_nor_gl_4k.png");

            var textureMetallic = ETexture.LoadImage("PbrGoldAlbedo", "Images/Pbr/Gold/gold-scuffed_metallic.png");
            //var textureMetallic = ETexture.LoadImage("PbrGoldAlbedo", "Images/Pbr/Iron/rustediron2_metallic.png");
            //var textureMetallic = ETexture.LoadImage("PbrGoldAlbedo", "Images/Pbr/Floor/wood_floor_worn_ao_4k.png");

            var textureRoughness = ETexture.LoadImage("PbrGoldAlbedo", "Images/Pbr/Gold/gold-scuffed_roughness.png");
            //var textureRoughness = ETexture.LoadImage("PbrGoldAlbedo", "Images/Pbr/Iron/rustediron2_roughness.png");
            //var textureRoughness = ETexture.LoadImage("PbrGoldAlbedo", "Images/Pbr/Floor/wood_floor_worn_rough_4k.png");

            var textureAo = ETexture.LoadImage("PbrGoldAo", "Images/Pbr/Default/Ao.png");

            PbrMaterialNew material = new PbrMaterialNew();
			material.name = "PbrGold";
            material.albedo = Vector3.One;
            material.albedoMap = textureAlbedo;
            material.metallicMap = textureMetallic;
            material.roughnessMap = textureRoughness;
            material.aoMap = textureAo;

			return material;
        }
	}
}