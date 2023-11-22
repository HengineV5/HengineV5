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

		static void Create(Main world, Vector3 pos)
		{
			var objRef = world.Create(new Entity2());
			Entity2.Ref ball = world.Get(objRef);
			ball.Position.Set(pos);
			ball.Scale.Set(Vector3.One);
			ball.Rotation.Set(Quaternion.Identity);
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

			var meshBall = Mesh.LoadOBJ("Models/Ball.obj");
			var meshBox = Mesh.LoadOBJ("Models/Box.obj");

			// Vulkan
			var shader = Shader.FromFiles("Shaders/VulkanVert.spv", "Shaders/VulkanFrag.spv");

			Create(mainWorld, new(0, 0, -5));
			//Create(mainWorld,  new(3, 0, -5));

			// OpenGL
			/*
			var gl = engine.argGL;

			var shader = Engine.Graphics.Shader.FromFiles("Shaders/Shader.vert", "Shaders/Shader.frag");
			var shaderProgram = shader.CreateProgram(gl);

			var vaoBall = meshBall.CreateVertexArray(gl);
			var vbBall = meshBall.CreateVertexBuffer(gl);
			var ebBall = meshBall.CreateElementBuffer(gl);

			var vaoBox = meshBox.CreateVertexArray(gl);
			var vbBox = meshBox.CreateVertexBuffer(gl);
			var ebBox = meshBox.CreateElementBuffer(gl);

			Create(mainWorld, shaderProgram, vaoBall, new(0, 0, -5));
			Create(mainWorld, shaderProgram, vaoBox, new(3, 0, -5));

			engine.argIWindow.Dispose();
			*/
			engine.Start();
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