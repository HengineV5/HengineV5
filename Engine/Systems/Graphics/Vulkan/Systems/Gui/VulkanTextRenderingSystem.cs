using EnCS.Attributes;
using Engine.Components;
using Engine.Graphics;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using System.Numerics;

namespace Engine
{
	[System]
	[SystemContext<VulkanRenderContext>]
	[UsingResource<VulkanTextureAtlasResourceManager>]
	[UsingResource<VulkanTextResourceManager>]
	public partial class VulkanTextRenderingSystem
	{
		VkContext context;
		VkRenderContext renderContext;
		IWindow window;

		Sampler sampler;

		public VulkanTextRenderingSystem(VkContext context, VkRenderContext renderContext, IWindow window)
		{
			this.context = context;
			this.renderContext = renderContext;
			this.window = window;
		}

		public void Init()
		{
			sampler = VulkanHelper.CreateSampler(context, 5);
		}

		// TODO: Refactor out
		int bufferIdx;
		int updateIdx;

		[SystemPreLoop, SystemLayer(0, 2)]
		public void PreRenderPass()
		{
			renderContext.pipeline.StartRenderPass(context, RenderPassId.Gui, PipelineContainerLayer.Gui);

			bufferIdx = 0;
			updateIdx = 0;
		}

		[SystemUpdate, SystemLayer(0, 2)]
		public void BufferUpdate(ref VulkanRenderContext context, GuiProperties.Ref properties, GuiPosition.Ref position, ref VkTextBuffer text, ref VkTextureAtlas textureAtlas)
		{
			ref GuiShaderInput shaderInput = ref renderContext.pipeline.GetUbo<GuiShaderInput>(bufferIdx);
			shaderInput.ubo.Value = context.guiUbo;

			shaderInput.ubo.Value.position = new Vector4(position.x, position.y, position.z, position.w);
			shaderInput.ubo.Value.size = new Vector4(10, 0, 10, 0);
			shaderInput.guiState.Value.totalStates = textureAtlas.textures;

			VulkanRenderHelpers.UpdateGuiDescriptorSet(this.context, renderContext.pipeline.GetDescriptorSet(PipelineContainerLayer.Gui, bufferIdx), textureAtlas.atlas, sampler);
		}

		[SystemUpdate, SystemLayer(0, 2)]
		public void RenderUpdate(ref VulkanRenderContext context, GuiProperties.Ref properties, GuiPosition.Ref position, ref VkTextBuffer text, ref VkTextureAtlas textureAtlas)
		{
			RenderMesh(text);
		}

		[SystemPostLoop, SystemLayer(0, 2)]
		public void PostRenderPass()
		{
			renderContext.pipeline.EndRenderPass(context);
		}

		void RenderMesh(VkTextBuffer textBuffer)
		{
			renderContext.pipeline.Render(this.context, PipelineContainerLayer.Gui, textBuffer.vertexBuffer, textBuffer.indexBuffer, textBuffer.indicies, updateIdx);
		}
	}
}
