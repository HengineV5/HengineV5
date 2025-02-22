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
			gl.Enable(EnableCap.CullFace);
			gl.Enable(EnableCap.FramebufferSrgb);

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

		Vector2f GetMousePosition();

		void SetCursorPosition(Vector2f pos);

		void HideCursor();

		void ShowCursor();
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

		public Vector2f GetMousePosition()
		{
			return new(mouse.Position.X, mouse.Position.Y);
		}

		public void SetCursorPosition(Vector2f pos)
		{
			mouse.Position = new(pos.x, pos.y);
		}

		public void HideCursor()
		{
			mouse.Cursor.CursorMode = CursorMode.Disabled;
		}

		public void ShowCursor()
		{
			mouse.Cursor.CursorMode = CursorMode.Normal;
		}
	}
}
