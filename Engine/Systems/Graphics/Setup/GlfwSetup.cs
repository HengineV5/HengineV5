using Silk.NET.Input;
using Silk.NET.Input.Glfw;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Glfw;
using System.Numerics;

namespace Engine
{

	public static class GlfwSetup
	{
		public static WindowOptions OpenGLWindowSetup()
		{
			return WindowOptions.Default;
		}

		public static WindowOptions VulkanWindowSetup()
		{
			return WindowOptions.DefaultVulkan;
		}

		public static Vk VulkanSetup()
		{
			return Vk.GetApi();
		}

		public static GL OpenGLSetup(IWindow window)
		{
			GL gl = GL.GetApi(window);

			gl.Enable(EnableCap.DepthTest);
			gl.ClearColor(System.Drawing.Color.CornflowerBlue);

			return gl;
		}

		public static IWindow WindowSetup(WindowOptions options, EngineConfig config)
		{
			GlfwWindowing.RegisterPlatform();
			GlfwInput.RegisterPlatform();

			options.Size = new Vector2D<int>(800, 600);
			options.Title = config.appName;

			IWindow window = Window.Create(options);
			window.Initialize();

			return window;
		}

		public static IInputHandler InputSetup(IWindow window)
		{
			return new GlfwInputHandler(window);
		}
	}

	public interface IInputHandler
	{
		void PollEvents();

		bool IsKeyDown(Key key);

		bool IsKeyDown(MouseButton key);

		Vector2 GetMousePosition();
	}

	public class GlfwInputHandler : IInputHandler
	{
		IWindow window;
		IInputContext inputContext;

		IMouse mouse;
		IKeyboard keyboard;

		public GlfwInputHandler(IWindow window)
		{
			this.window = window;
			this.inputContext = window.CreateInput();
			this.keyboard = inputContext.Keyboards.FirstOrDefault();
			this.mouse = inputContext.Mice.FirstOrDefault();
		}

		public void PollEvents()
		{
			window.DoEvents();
		}

		public bool IsKeyDown(Key key)
		{
			return keyboard.IsKeyPressed(key);
		}

		public bool IsKeyDown(MouseButton key)
		{
			return mouse.IsButtonPressed(key);
		}

		public Vector2 GetMousePosition()
		{
			return mouse.Position;
		}
	}
}
