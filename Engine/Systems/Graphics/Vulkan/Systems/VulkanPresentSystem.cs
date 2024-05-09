using EnCS.Attributes;
using Engine.Graphics;
using Silk.NET.Windowing;

namespace Engine
{
	[System]
	[SystemContext<VulkanRenderContext>]
	public partial class VulkanPresentSystem
	{
		IWindow window;
		VkContext context;
		VkRenderContext renderContext;

		public VulkanPresentSystem(VkContext context, VkRenderContext renderContext, IWindow window)
		{
			this.context = context;
			this.renderContext = renderContext;
			this.window = window;
		}

		[SystemUpdate]
		public void UpdateCamera(ref VulkanRenderContext context, Camera.Ref camera)
		{
		}

		public void PostRun()
		{
			renderContext.pipeline.PresentRender(context);
		}
	}
}
