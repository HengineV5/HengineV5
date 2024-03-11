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
using Engine.Parsing;

namespace Engine
{
	public class EngineConfig
	{
		public string appName;
		public Version appVersion;

		public string engineName;
		public Version engineVersion;

		public int idx;
	}

	public struct EngineContext
	{
		public float dt;
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
					//x.ArchType<Position, Rotation, Scale, Mesh, ETexture>("Entity");
					x.ArchType<Position, Rotation, Scale, Mesh, PbrMaterial, Networked>("NEntity");
					x.ArchType<Position, Rotation, Scale, HexCell, Mesh, PbrMaterial, Networked>("Hex");
					x.ArchType<Position, Rotation, Camera, Skybox, Networked>("Cam");
					x.ArchType<Position, Rotation, Scale, GizmoComp>("Gizmo");
				})
				.System(x =>
				{
					x.System<PositionSystem>();
					x.System<RotateSystem>();
					x.System<MoveSystem>();

					x.System<OpenGLRenderSystem>();

					x.System<VulkanCameraSystem>();
					x.System<VulkanPbrRenderSystem>();
					x.System<VulkanWireframeRenderSystem>();
					x.System<VulkanPresentSystem>();

					x.System<ClientSendSystem>();
					x.System<ClientReceiveSystem>();
				})
				.World(x =>
				{
					x.World<HengineEcs.NEntity, HengineEcs.Cam, HengineEcs.Hex>("Main");
				})
				.Resource(x =>
				{
					x.ResourceManager<VulkanMeshResourceManager>();
					x.ResourceManager<VulkanTextureResourceManager>();
					x.ResourceManager<VulkanMaterialResourceManager>();
					x.ResourceManager<VulkanSkyboxResourceManager>();

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
					x.WithConfig<NetworkConfig>();

					//x.Setup(ImageFormatSetup.HdrSetup);

					//x.Setup(GlfwSetup.OpenGLWindowSetup);
					x.Setup(GlfwSetup.VulkanWindowSetup);

					x.Setup(GlfwSetup.WindowSetup);
					x.Setup(GlfwSetup.InputSetup);

					//x.Setup(GlfwSetup.OpenGLSetup);

					x.Setup(GlfwSetup.VulkanSetup);
					x.Setup(VulkanSetup.RenderSetup);

					//x.Setup(NetworkSetup.ClientSetup);
				})
				.Resource(x =>
				{
					x.ResourceManager<VulkanMeshResourceManager>();
					x.ResourceManager<VulkanTextureResourceManager>();
                    x.ResourceManager<VulkanMaterialResourceManager>();
                    x.ResourceManager<VulkanSkyboxResourceManager>();

                    //x.ResourceManager<OpenGLMeshResourceManager>();
                    //x.ResourceManager<OpenGLTextureResourceManager>();
                })
				.Layout(x =>
				{
					x.Pipeline("Graphics", x =>
					{
						//x.Sequential<OpenGLRenderSystem>();

						x.Sequential<VulkanCameraSystem>();
						x.Sequential<VulkanPbrRenderSystem>();
						x.Sequential<VulkanWireframeRenderSystem>();
						x.Sequential<VulkanPresentSystem>();
					});

					x.Pipeline("Rotate", x =>
					{
						//x.Sequential<RotateSystem>();
						x.Sequential<MoveSystem>();

						//x.Sequential<ClientSendSystem>();
						//x.Sequential<ClientReceiveSystem>();
					});

					x.World("Main", x =>
					{
						x.Pipeline<Hengine.GraphicsPipeline>();
					});
				})
				.Build<Hengine, HengineEcs>();
		}
	}

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
					x.ArchType<Position, Rotation, Camera, Networked>("Cam");
				})
				.System(x =>
				{
					x.System<ServerSystem>();
				})
				.World(x =>
				{
					x.World<HengineServerEcs.Cam>("Main");
				})
				.Resource(x =>
				{
				})
				.Build<HengineServerEcs>();
		}

		public void HGine()
		{
			new HengineBuilder()
				.Config(x =>
				{
					x.WithConfig<EngineConfig>();
					x.WithConfig<NetworkConfig>();

					x.Setup(NetworkSetup.ServerSetup);
				})
				.Resource(x =>
				{
				})
				.Layout(x =>
				{
					x.Pipeline("Server", x =>
					{
						x.Sequential<ServerSystem>();
					});

					x.World("Main", x =>
					{
					});
				})
				.Build<HengineServer, HengineServerEcs>();
		}
	}
}
