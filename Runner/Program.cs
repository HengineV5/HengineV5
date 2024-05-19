using CommunityToolkit.HighPerformance;
using Engine;
using Engine.Components;
using Engine.Graphics;
using Engine.Parsing;
using Engine.Utils.Parsing.TTF;
using System.Net;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using static Engine.HengineEcs;

namespace Runner
{
	internal class Program
	{
		static EnCS.ArchRef<Cam> camRef;

		static void Main(string[] args)
		{
			/*
			unsafe
			{
				GuiUniformBufferObject gubo = new();
				byte* addr = (byte*)&gubo;

				Console.WriteLine("Size:      {0}", sizeof(GuiUniformBufferObject));
				Console.WriteLine("Proj Offset: {0}", (byte*)&gubo.proj - addr);
				Console.WriteLine("Screen Offset: {0}", (byte*)&gubo.screenSize - addr);
				Console.WriteLine("Position Offset: {0}", (byte*)&gubo.position - addr);
				Console.WriteLine("Size Offset: {0}", (byte*)&gubo.size - addr);
            }
			*/

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
			Overlay overlayWorld = ecs.GetOverlay();

			var skybox = Skybox.LoadSkybox("Skybox", "Images/Skybox/Default");
			Camera camera = new Camera
			{
				width = engine.argIWindow.Size.X,
				height = engine.argIWindow.Size.Y,
				fov = 1.22173f, // 70 degrees
				zNear = 0.1f,
				zFar = 1000
			};

			//var meshDuck = Mesh.LoadGltf("Duck", "Models/Duck/Duck.gltf", true);
			//var materialDuck = PbrMaterial.LoadGltf("Duck", "Models/Duck/Duck.gltf");

            //mainWorld.CreateObject(new(3, 0, -10), Vector3.One, meshDuck, materialDuck, engineConfig.idx == 10 ? 11 : 10);
			camRef = mainWorld.CreateCamera(camera, Vector3.Zero, skybox, engineConfig.idx);

			engine.argIWindow.FramebufferResize += x =>
			{
				Main mainWorld = ecs.GetMain();
				var cam = mainWorld.Get(camRef);

				Camera camera = new Camera
				{
					width = x.X,
					height = x.Y,
					fov = 1.22173f, // 70 degrees
					zNear = 0.1f,
					zFar = 1000
				};

				cam.Camera.Set(camera);
			};

			var font = TtfLoader.LoadFont("Fonts/arial.ttf");

            var buttonAtlas = TextureAtlas.LoadAtlas("ButtonAtlas", 3, "Images/Gui/Button/Button.png");

			GuiProperties prop = new GuiProperties()
			{
				shape = GuiShape.Circle
			};
			overlayWorld.CreateGuiElement(new(30, 0, 250, 0), new(50 * 4, 0, 50 * 4, 0), buttonAtlas, prop);

			string str = "AaEeRr";

            for (int a = 0; a < str.Length; a++)
            {
				//var glyph = font.glyphData[a];
				var glyph = font.GetGlyphIndex(str[a]);
				Vector2 pos = new Vector2(50, 50) + Vector2.UnitX * a * 100;

                Console.WriteLine($"{str[a]}");
				for (int i = 0; i < glyph.endPtsOfContours.Length; i++)
				{
                    Console.WriteLine($"	{glyph.endPtsOfContours[i]}");
                }

                for (int i = 0; i < glyph.xCoords.Length; i++)
				{
					//Console.WriteLine($"X: {glyph.xCoords[i]}, Y: {glyph.yCoords[i]}");
					// Invert Y axis as Vulkan y points down.
					Vector2 xy = new Vector2(glyph.glyphDescription.xMax - glyph.xCoords[i], glyph.glyphDescription.yMax - glyph.yCoords[i]);
					xy /= 20f;
					xy += pos;

					overlayWorld.CreateGuiElement(new(xy.X, 0, xy.Y, 0), new(5, 0, 5, 0), buttonAtlas, new() { shape = GuiShape.Box });
				}
			}
			/*
			*/

            TestWorld.Load(mainWorld);
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