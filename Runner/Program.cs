using Engine;
using Engine.Components.Graphics;
using Engine.Graphics;
using Silk.NET.Maths;
using Silk.NET.Windowing;
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
		*/

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

		static void CreateCamera(Main world, Camera camera, Vector3 position)
		{
			var objRef = world.Create(new Cam());
			Cam.Ref entRef = world.Get(objRef);
			entRef.Camera.Set(camera);
			entRef.Position.Set(position);
			entRef.Rotation.Set(Quaternion.Identity);
		}

		static void Main(string[] args)
		{
			var engineConfig = new EngineConfig()
			{
				appName = "Hengine v5",
				appVersion = new Version(0, 0, 1),

				engineName = "Hengine",
				engineVersion = new Version(5, 0, 0)
			};

			var vulkanConfig = new VulkanConfig()
			{
				validationLayers = ["VK_LAYER_KHRONOS_validation"]
			};

			Hengine engine = new Hengine();
			engine.Initialize(engineConfig, vulkanConfig);

			var ecs = engine.GetEcs();
			Main mainWorld = ecs.GetMain();

			var meshBall = Mesh.LoadOBJ("Ball", "Models/Ball.obj");
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

			Create(mainWorld, new(3, 0, -5), meshBox, texture);
			Create(mainWorld, new(0, 0, -5), meshBall, texture);
			Create(mainWorld, new(-3, 0, -5), meshBall, texture2);

			engine.Start();
			engine.argIWindow.Dispose();
		}

		static IWindow CreateWindow()
		{
			var options = WindowOptions.Default;
			options.Size = new Vector2D<int>(800, 600);
			options.Title = "Hengine v5";

			return Window.Create(options);
		}
	}
}