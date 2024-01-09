using Engine;
using Engine.Components;
using Engine.Components.Graphics;
using Engine.Graphics;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using System.Net;
using System.Numerics;
using static Engine.HengineEcs;

namespace Runner
{
	internal class Program
	{
		/*
		static void Create(Main world, ShaderProgram shader, VertexArrayObject vao, Vector3 pos)
		{
			var objRef = world.Create(new Entity());
			Entity.Ref ball = world.Get(objRef);
			ball.Position.Set(pos);
			ball.Scale.Set(Vector3.One);
			ball.Rotation.Set(Quaternion.Identity);

			ball.ShaderProgram.Set(shader);
			ball.VertexArrayObject.Set(vao);
		}

		static void Create(Main world, Vector3 pos, Mesh mesh, ETexture texture)
		{
			var objRef = world.Create(new Entity());
			Entity.Ref entRef = world.Get(objRef);
			entRef.Position.Set(pos);
			entRef.Scale.Set(Vector3.One);
			entRef.Rotation.Set(Quaternion.Identity);
			entRef.Mesh.Set(mesh);
			entRef.ETexture.Set(texture);
		}
		*/

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

			var meshBall = Mesh.LoadOBJ("Ball", "Models/Ball.obj");
			var meshSphere = Mesh.LoadOBJ("Sphere", "Models/SphereSmooth.obj");
			var meshBox = Mesh.LoadOBJ("Box", "Models/Box.obj");

			var texture = ETexture.LoadImage("Haakon", "Images/image_2.png");
			var texture2 = ETexture.LoadImage("Statue", "Images/image.png");

			Camera camera = new Camera
			{
				width = 800,
				height = 600,
				fov = 1.22173f, // 70 degrees
				zNear = 0.1f,
				zFar = 1000
			};

			CreateCamera(mainWorld, camera, Vector3.Zero);

			/*
			Create(mainWorld, new(3, 0, -5), meshBox, texture, 1);
			Create(mainWorld, new(0, 0, -5), meshBall, texture, 0);
			Create(mainWorld, new(-3, 0, -5), meshSphere, texture2, 11);

			*/

			float midX = 32f / 2f;
			float midY = 4f / 2f;

			for (int y = 0; y < 32; y++)
			{
				for (int x = 0; x < 32; x++)
				{
					Create(mainWorld, new(-midX + x * 2.1f, -midY + y * 2.1f, -5), meshSphere, texture, 1);
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
	}
}