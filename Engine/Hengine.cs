using EnCS;
using EnCS.Attributes;
using Engine.Components;
using Engine.Components.Graphics;
using Silk.NET.GLFW;
using Silk.NET.Windowing;
using Silk.NET.Vulkan;
using Silk.NET.OpenGL;
using System.Drawing;
using System.Runtime.InteropServices;
using Engine.Graphics;

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
		[SystemUpdate]
		public void Update(Position.Ref position)
		{
		}

		[SystemUpdate]
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
					x.ArchType<Position, Rotation, Scale, Mesh, ETexture>("Entity");
					x.ArchType<Position, Rotation, Camera>("Cam");
				})
				.System(x =>
				{
					x.System<PositionSystem>();
					x.System<RotateSystem>();
					x.System<OpenGLRenderSystem>();
					x.System<VulkanRenderSystem>();
					x.System<MoveSystem>();
				})
				.World(x =>
				{
					x.World<HengineEcs.Entity, HengineEcs.Cam>("Main");
				})
				.Resource(x =>
				{
					x.ResourceManager<VulkanMeshResourceManager>();
					x.ResourceManager<VulkanTextureResourceManager>();

					//x.ResourceManager<OpenGLMeshResourceManager>();
					//x.ResourceManager<OpenGLTextureResourceManager>();
				})
				.Build<HengineEcs>();
		}

		public void HGine()
		{
			new HengineBuilder()
				.Config(x =>
				{
					x.WithConfig<EngineConfig>();
					x.WithConfig<VulkanConfig>();

					//x.Setup(GlfwSetup.OpenGLWindowSetup);
					x.Setup(GlfwSetup.VulkanWindowSetup);

					x.Setup(GlfwSetup.WindowSetup);
					x.Setup(GlfwSetup.InputSetup);

					//x.Setup(GlfwSetup.OpenGLSetup);

					x.Setup(GlfwSetup.VulkanSetup);
					x.Setup(VulkanSetup.RenderSetup);
				})
				.Resource(x =>
				{
					x.ResourceManager<VulkanMeshResourceManager>();
					x.ResourceManager<VulkanTextureResourceManager>();

					//x.ResourceManager<OpenGLMeshResourceManager>();
					//x.ResourceManager<OpenGLTextureResourceManager>();
				})
				.Layout(x =>
				{
					x.Pipeline("Graphics", x =>
					{
						//x.Sequential<OpenGLRenderSystem>();
						x.Sequential<VulkanRenderSystem>();
					});

					x.Pipeline("Rotate", x =>
					{
						//x.Sequential<RotateSystem>();
						x.Sequential<MoveSystem>();
					});

					x.World("Main", x =>
					{
						x.Pipeline<Hengine.GraphicsPipeline>();
					});
				})
				.Build<Hengine, HengineEcs>();
		}
	}

	/*
	public partial class HengineServerEcs
	{

	}


	public partial class HengineServer
	{
		bool ShouldExit()
		{
			return false;
		}

		public void Ecs()
		{
			new EcsBuilder()
				.ArchType(x =>
				{
					x.ArchType<Position, Rotation, Scale, ShaderProgram, VertexArrayObject>("Entity");
					x.ArchType<Position, Rotation, Scale, Mesh, Texture>("Entity2");
					x.ArchType<Position, Rotation, Camera>("Cam");
				})
				.System(x =>
				{
				})
				.World(x =>
				{
					x.World<HengineEcs.Entity, HengineEcs.Entity2, HengineEcs.Cam>("Main");
				})
				.Resource(x =>
				{
					x.ResourceManager<MeshResourceManager>();
					x.ResourceManager<TextureResourceManager>();
				})
				.Build<HengineServerEcs>();
		}

		public void HGine()
		{
			new HengineBuilder()
				.Config(x =>
				{
					x.WithConfig<EngineConfig>();
				})
				.Resource(x =>
				{
				})
				.Layout(x =>
				{
					x.World("Main", x =>
					{
					});
				})
				.Build<HengineServer, HengineEcs>();
		}
	}
	*/
}
