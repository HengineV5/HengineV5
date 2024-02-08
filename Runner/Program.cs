using Engine;
using Engine.Components;
using Engine.Components.Graphics;
using Engine.Graphics;
using Engine.Parsing;
using Silk.NET.Maths;
using Silk.NET.OpenAL;
using Silk.NET.Windowing;
using System.Dynamic;
using System.Net;
using System.Numerics;
using static Engine.HengineEcs;

namespace Runner
{
	internal class Program
	{
        static void Create(Main world, Vector3 pos, Mesh mesh, PbrMaterial material, int idx)
        {
            var objRef = world.Create(new NEntity());
            NEntity.Ref entRef = world.Get(objRef);
            entRef.Position.Set(pos);
            entRef.Scale.Set(Vector3.One);
            entRef.Rotation.Set(Quaternion.Identity);
            entRef.Mesh.Set(mesh);
            entRef.PbrMaterial.Set(material);
            entRef.Networked.Set(new Networked()
            {
                idx = idx
            });
        }

        static void CreateCamera(Main world, Camera camera, Vector3 position, in Skybox skybox)
		{
			var objRef = world.Create(new Cam());
			Cam.Ref entRef = world.Get(objRef);
			entRef.Camera.Set(camera);
			entRef.Position.Set(position);
			entRef.Rotation.Set(Quaternion.Identity);
			entRef.Skybox.Set(skybox);
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

			var skybox = Skybox.LoadSkybox("Skybox", "Images/Skybox/Default");
            Camera camera = new Camera
            {
                width = 800,
                height = 600,
                fov = 1.22173f, // 70 degrees
                zNear = 0.1f,
                zFar = 1000
            };

            //var meshBox = Mesh.LoadGltf("Box", "Models/Box/box.gltf");
            var meshDuck = Mesh.LoadGltf("Duck", "Models/Duck/Duck.gltf", true);
			var materialDuck = PbrMaterial.LoadGltf("Duck", "Models/Duck/Duck.gltf");

			var meshMap = GetMapMesh();

			//var meshBall = Mesh.LoadOBJ("Ball", "Models/Ball.obj");
			var meshSphere = Mesh.LoadOBJ("Sphere", "Models/SphereSmooth.obj");
			var materialSphere = GetMaterial();
			//var meshBox = Mesh.LoadOBJ("Box", "Models/Box.obj");

			//var texture = ETexture.LoadImage("Haakon", "Images/image_2.png");
			//var texture2 = ETexture.LoadImage("Statue", "Images/image.png");

			CreateCamera(mainWorld, camera, Vector3.Zero, skybox);

			Create(mainWorld, new(3, 0, -10), meshDuck, materialDuck, 1);
			Create(mainWorld, new(3, 0, 10), meshMap, materialDuck, 1);

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

		static PbrMaterial GetMaterial()
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

            var textureNormal = ETexture.LoadImage("PbrGoldAlbedo", "Images/Pbr/Gold/gold-scuffed_normal.png");
            //var textureNormal = ETexture.LoadImage("PbrGoldAlbedo", "Images/Pbr/Iron/rustediron2_normal.png");
            //var textureNormal = ETexture.LoadImage("PbrGoldAlbedo", "Images/Pbr/Floor/wood_floor_worn_nor_gl_4k.png");

            PbrMaterial material = new PbrMaterial();
			material.name = "PbrGold";
            material.albedo = Vector3.One;
            material.albedoMap = textureAlbedo;
            material.metallicMap = textureMetallic;
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
			vericies[0] = new Vertex(new(0, 0, OUTER_RADIUS), Vector3.UnitY, new(0.5f, 0));
			vericies[1] = new Vertex(new(INNER_RADIUS, 0, OUTER_RADIUS * 0.5f), Vector3.UnitY, new(1, 0.33f));
			vericies[2] = new Vertex(new(INNER_RADIUS, 0, -OUTER_RADIUS * 0.5f), Vector3.UnitY, new(1, 0.66f));
			vericies[3] = new Vertex(new(0, 0, -OUTER_RADIUS), Vector3.UnitY, new(0.5f, 1));
			vericies[4] = new Vertex(new(-INNER_RADIUS, 0, -OUTER_RADIUS * 0.5f), Vector3.UnitY, new(0, 0.66f));
			vericies[5] = new Vertex(new(-INNER_RADIUS, 0, OUTER_RADIUS * 0.5f), Vector3.UnitY, new(0, 0.33f));

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

                verticeis.Add(new(new(posX + -INNER_RADIUS, 0, posY + -OUTER_RADIUS * 0.5f), Vector3.UnitY, new()));
                verticeis.Add(new(new(posX + -INNER_RADIUS, 0, posY + OUTER_RADIUS * 0.5f), Vector3.UnitY, new()));

                for (int x = 0; x < width; x++)
                {
                    posX = x * INNER_RADIUS * 2 + y * INNER_RADIUS;

                    if (y == height - 1)
                        verticeis.Add(new(new(posX, 0, posY + OUTER_RADIUS), Vector3.UnitY, new(Random.Shared.NextSingle())));

                    if (y == 0)
						verticeis.Add(new(new(posX, 0, posY + -OUTER_RADIUS), Vector3.UnitY, new(Random.Shared.NextSingle())));

                    verticeis.Add(new(new(posX + INNER_RADIUS, 0, posY + -OUTER_RADIUS * 0.5f), Vector3.UnitY, new(Random.Shared.NextSingle())));
                    verticeis.Add(new(new(posX + INNER_RADIUS, 0, posY + OUTER_RADIUS * 0.5f), Vector3.UnitY, new(Random.Shared.NextSingle())));
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