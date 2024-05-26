using EnCS;
using EnCS.Attributes;
using Engine.Components;
using Engine.Components.Graphics;
using Silk.NET.GLFW;
using Silk.NET.Windowing;
using Silk.NET.Vulkan;
using Silk.NET.OpenGL;
using System.Runtime.InteropServices;
using Engine.Graphics;
using Engine.Parsing;
using Engine.Translation;

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

					x.ArchType<GuiProperties, GuiPosition, GuiSize, TextureAtlas>("GuiElement");
					x.ArchType<GuiProperties, GuiPosition, TextureAtlas, GuiText>("TextElement");
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
					x.System<VulkanGuiRenderSystem>();
					x.System<VulkanTextRenderingSystem>();

					x.System<ClientSendSystem>();
					x.System<ClientReceiveSystem>();
				})
				.World(x =>
				{
					x.World<HengineEcs.NEntity, HengineEcs.Cam, HengineEcs.Hex>("Main");
					x.World<HengineEcs.GuiElement, HengineEcs.TextElement>("Overlay");
				})
				.Resource(x =>
				{
					x.ResourceManager<VulkanMeshResourceManager>();
					x.ResourceManager<VulkanTextureResourceManager>();
					x.ResourceManager<VulkanTextResourceManager>();
					x.ResourceManager<VulkanMaterialResourceManager>();
					x.ResourceManager<VulkanSkyboxResourceManager>();
					x.ResourceManager<VulkanTextureAtlasResourceManager>();

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
					x.WithConfig<TranslationConfig>();

					x.Setup(Translator.TranslationSetup);
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
					x.ResourceManager<VulkanTextResourceManager>();
                    x.ResourceManager<VulkanMaterialResourceManager>();
                    x.ResourceManager<VulkanSkyboxResourceManager>();
					x.ResourceManager<VulkanTextureAtlasResourceManager>();

					//x.ResourceManager<OpenGLMeshResourceManager>();
					//x.ResourceManager<OpenGLTextureResourceManager>();
				})
				.Layout(x =>
				{
					x.Pipeline("Camera", x =>
					{
						x.Sequential<VulkanCameraSystem>();
					});

					x.Pipeline("Graphics", x =>
					{
						//x.Sequential<OpenGLRenderSystem>();

						//x.Sequential<VulkanCameraSystem>();
						x.Sequential<VulkanPbrRenderSystem>();
						x.Sequential<VulkanWireframeRenderSystem>();
						//x.Sequential<VulkanPresentSystem>();
					});

					x.Pipeline("Overlay", x =>
					{
						x.Sequential<VulkanGuiRenderSystem>();
						x.Sequential<VulkanTextRenderingSystem>();
					});

					x.Pipeline("Present", x =>
					{
						x.Sequential<VulkanPresentSystem>();
					});

					x.Pipeline("Rotate", x =>
					{
						//x.Sequential<RotateSystem>();
						x.Sequential<MoveSystem>();

						//x.Sequential<ClientSendSystem>();
						//x.Sequential<ClientReceiveSystem>();
					});

					x.World<HengineEcs.Main.Interface>(x =>
					{
						x.Pipeline<Hengine.CameraPipeline>();
					});

					x.World<HengineEcs.Main.Interface>(x =>
					{
						x.Pipeline<Hengine.GraphicsPipeline>();

						x.Pipeline<Hengine.RotatePipeline>();
					});

					x.World<HengineEcs.Overlay.Interface>(x =>
					{
						x.Pipeline<Hengine.OverlayPipeline>();
					});

					x.World<HengineEcs.Main.Interface>(x =>
					{
						x.Pipeline<Hengine.PresentPipeline>();
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

					x.World<HengineEcs.Main.Interface>(x =>
					{
					});
				})
				.Build<HengineServer, HengineServerEcs>();
		}
	}
}
