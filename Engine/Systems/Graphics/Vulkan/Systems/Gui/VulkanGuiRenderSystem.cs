using EnCS;
using EnCS.Attributes;
using Engine.Components;
using Engine.Graphics;
using Silk.NET.Windowing;
using System.Numerics;

namespace Engine
{
	[System<VulkanRenderContext>]
	public partial class VulkanGuiRenderSystem
	{
		IWindow window;
		VkContext context;
		VkRenderContext renderContext;

		public VulkanGuiRenderSystem(VkContext context, VkRenderContext renderContext, IWindow window)
		{
			this.context = context;
			this.renderContext = renderContext;
			this.window = window;
		}

		public void Init()
		{
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
		public void BufferUpdate(ref VulkanRenderContext context, Position.Ref position, Rotation.Ref rotation, Scale.Ref scale)
		{
			bufferIdx++;
		}

		[SystemUpdate, SystemLayer(0, 2)]
		public void RenderUpdate(ref VulkanRenderContext context, Position.Ref position, Rotation.Ref rotation, Scale.Ref scale)
		{
			updateIdx++;
		}

		[SystemPostLoop, SystemLayer(0, 2)]
		public void PostRenderPass()
		{
			renderContext.pipeline.EndRenderPass(context);
		}
	}
}
