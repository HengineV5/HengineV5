using CommunityToolkit.HighPerformance;
using Engine;
using Engine.Components;
using Engine.Graphics;
using Engine.Parsing;
using Engine.Translation;
using Engine.Utils.Parsing.TTF;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Vertical.SpectreLogger;
using Vertical.SpectreLogger.Options;
using static Engine.HengineEcs;

namespace Runner
{
	static partial class LoggerExtensionMethods
	{
		[LoggerMessage(Level = LogLevel.Information, Message = "{engineName} v{engineVersion} started with app {appName} v{appVersion}")]
		public static partial void LogEngineStarted(this ILogger logger, string engineName, string engineVersion, string appName, string appVersion);

		[LoggerMessage(Level = LogLevel.Information, Message = "Client started with id {id}")]
		public static partial void LogClientStarted(this ILogger logger, int id);

		[LoggerMessage(Level = LogLevel.Information, Message = "Server started on {ip}:{port}")]
		public static partial void LogServerStarted(this ILogger logger, string ip, int port);
	}

	internal class Program
	{
		static EnCS.ArchRef<Cam> camRef;

		static void Main(string[] args)
		{
			using ILoggerFactory factory = LoggerFactory.Create(builder =>
			{
				builder.AddSpectreConsole(x =>
				{
					x.SetMinimumLevel(LogLevel.Trace);

					//x.UseSerilogConsoleStyle();
					//x.UseMicrosoftConsoleStyle();

					x.SetMinimumLevel("Engine.Server", LogLevel.Debug);
					x.SetMinimumLevel("Engine.Client", LogLevel.Debug);
					x.SetMinimumLevel("Engine.ClientSendSystem", LogLevel.Debug);
					x.SetMinimumLevel("Engine.ClientReceiveSystem", LogLevel.Debug);
					x.SetMinimumLevel("ImageLib.Png.PngFormat", LogLevel.Warning);
				});

				builder.SetMinimumLevel(LogLevel.Trace);
				//builder.AddFilter((x, y) => true);
			});

			var engineConfig = new EngineConfig()
			{
				appName = "Tower Shooter Survival",
				appVersion = new Version(0, 0, 1),

				engineName = "Hengine",
				engineVersion = new Version(5, 0, 0),

				idx = 0
			};

			ILogger logger = factory.CreateLogger("Program.Engine");
			logger.LogEngineStarted(engineConfig.engineName, engineConfig.engineVersion.ToString(), engineConfig.appName, engineConfig.appVersion.ToString());

			if (args.Length > 0 && args[0] == "server")
			{
				Server(factory, engineConfig);
			}
			else
			{
				if (args.Length > 1)
					engineConfig.idx = int.Parse(args[1]);

				Client(factory, engineConfig);
			}
		}

		static void Client(ILoggerFactory factory, EngineConfig engineConfig)
		{
			ILogger logger = factory.CreateLogger("Program.Client");
			logger.LogClientStarted(engineConfig.idx);

			var vulkanConfig = new VulkanConfig()
			{
				validationLayers = ["VK_LAYER_KHRONOS_validation"]
			};

			var networkConfig = new NetworkConfig()
			{
				ipAddress = IPAddress.Parse("127.0.0.1"),
				port = 45567
			};

			TranslationUnit unit = new TranslationUnit(new Dictionary<string, string>
			{
				//{ "en-en", "This is a test text!" }
				{ "en-en", "The quick brown fox jumps over the lazy dog." }
			});

			Dictionary<string, TranslationUnit> tranlations = new Dictionary<string, TranslationUnit>()
			{
				{ "test_id", unit }
			};

			var translationConfig = new TranslationConfig()
			{
				language = "en-en",
				units = tranlations
			};
			
			Hengine engine = new Hengine(factory);
			engine.Initialize(engineConfig, vulkanConfig, networkConfig, translationConfig);

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

			var meshDuck = Mesh.LoadGltf("Duck", "Models/Duck/Duck.gltf", true);
			var materialDuck = PbrMaterial.LoadGltf("Duck", "Models/Duck/Duck.gltf");

			//Console.WriteLine(materialDuck.albedoMap.data.Width);

            mainWorld.CreateObject(new(3, 0, -10), Vector3f.One, meshDuck, materialDuck, engineConfig.idx == 10 ? 11 : 10);
			camRef = mainWorld.CreateCamera(camera, Vector3f.Zero, skybox, engineConfig.idx);

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
            var textAtlas = TextureAtlas.LoadAtlas("TextAtlas", 1, "Images/Gui/Text/Text.png");

			GuiProperties prop = new GuiProperties()
			{
				shape = GuiShape.Box
			};
			//overlayWorld.CreateGuiElement(new(30, 0, 250, 0), new(50 * 4, 0, 50 * 4, 0), buttonAtlas, prop);

			GuiText text = new GuiText()
			{
				id = "test_id",
				font = font,
			};
			//overlayWorld.CreateTextElement(new(30, 0, 50, 0), textAtlas, text);

			/*
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
			*/

            //TestWorld.Load(mainWorld);
			MapWorld.Load(factory.CreateLogger("MapWorld"), mainWorld);

			engine.Start();
			engine.argIWindow.Dispose();
		}

		static void Server(ILoggerFactory factory, EngineConfig engineConfig)
		{
			var networkConfig = new NetworkConfig()
			{
				ipAddress = IPAddress.Parse("127.0.0.1"),
				port = 45567
			};

			ILogger logger = factory.CreateLogger("Program.Client");
			logger.LogServerStarted(networkConfig.ipAddress.ToString(), networkConfig.port);

			HengineServer server = new HengineServer(factory);
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

			mainWorld.CreateCamera(camera, Vector3f.Zero, 10);
			mainWorld.CreateCamera(camera, Vector3f.Zero, 11);

			server.Start();
		}
	}
}