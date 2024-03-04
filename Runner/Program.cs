using Engine;
using Engine.Graphics;
using Engine.Parsing;
using System.Net;
using System.Numerics;
using static Engine.HengineEcs;

namespace Runner
{
	internal class Program
	{
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

			var meshDuck = Mesh.LoadGltf("Duck", "Models/Duck/Duck.gltf", true);
			var materialDuck = PbrMaterial.LoadGltf("Duck", "Models/Duck/Duck.gltf");

            Console.WriteLine(engineConfig.idx);
            mainWorld.CreateObject(new(3, 0, -10), Vector3.One, meshDuck, materialDuck, engineConfig.idx == 10 ? 11 : 10);
			mainWorld.CreateCamera(camera, Vector3.Zero, skybox, engineConfig.idx);

			//TestWorld.Load(mainWorld);
			//MapWorld.Load(mainWorld);

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

			mainWorld.CreateCamera(camera, Vector3.Zero, 10);
			mainWorld.CreateCamera(camera, Vector3.Zero, 11);

			server.Start();
		}
	}
}