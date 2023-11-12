using Silk.NET.Input.Glfw;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Glfw;

namespace Engine
{

	public static class GlfwSetup
	{
		public static (Vk, WindowOptions) VulkanSetup()
		{
			Vk vk = Vk.GetApi();

			return (vk, WindowOptions.DefaultVulkan);
		}

		public static WindowOptions OpenGLWindowSetup()
		{
			return WindowOptions.Default;
		}

		public static (GL, WindowOptions) OpenGLSetup(IWindow window)
		{
			GL gl = GL.GetApi(window);

			gl.Enable(EnableCap.DepthTest);
			gl.ClearColor(System.Drawing.Color.CornflowerBlue);

			return (gl, WindowOptions.Default);
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
	}
}
