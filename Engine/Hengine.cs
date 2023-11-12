using EnCS;
using EnCS.Attributes;
using Engine.Components;
using Engine.Components.Graphics;
using Silk.NET.GLFW;
using Silk.NET.Windowing;
using Silk.NET.Vulkan;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Engine
{
	public class EngineConfig
	{
		public string appName;
		public Version appVersion;

		public string engineName;
		public Version engineVersion;
	}

	[System]
	public partial class PositionSystem
	{
		public void Update(Position.Ref position)
		{
		}

		public void Update(ref Position.Vectorized position)
		{
		}
	}

	public partial class HengineEcs
	{

	}

	public partial class Hengine
	{
		bool ShouldExit()
		{
			return argIWindow.IsClosing;
		}

		static void Ecs()
		{
			new EcsBuilder()
				.ArchType(x =>
				{
					x.ArchType<Position, Rotation, Scale, ShaderProgram, VertexArrayObject>("Entity");
				})
				.System(x =>
				{
					x.System<PositionSystem>();
					x.System<RotateSystem>();
					x.System<OpenGLRenderSystem>();
					x.System<VulkanRenderSystem>();
				})
				.World(x =>
				{
					x.World<HengineEcs.Entity>("Main");
				})
				.Build<HengineEcs>();
		}

		public void HGine()
		{
			new HengineBuilder()
				.Config(x =>
				{
					x.WithConfig<EngineConfig>();

					//x.Setup(GlfwSetup.OpenGLSetup);
					x.Setup(GlfwSetup.VulkanSetup);
					x.Setup(GlfwSetup.WindowSetup);
				})
				.Layout(x =>
				{
					x.Pipeline("Graphics", x =>
					{
						//x.Sequential<OpenGLRenderSystem>();
					});

					x.Pipeline("Vulkan", x =>
					{
						x.Sequential<VulkanRenderSystem>();
					});

					x.Pipeline("Rotate", x =>
					{
						x.Sequential<RotateSystem>();
					});

					x.World("Main", x =>
					{
						x.Pipeline<Hengine.VulkanPipeline>();
					});
				})
				.Build<Hengine, HengineEcs>();
		}
	}
}
