using EnCS.Attributes;
using Hengine.Graphics;
using Silk.NET.Windowing;

namespace Hengine
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
		public void UpdateCamera(ref VulkanRenderContext context, ref Camera camera)
		{
		}

		public void PostRun()
		{
			renderContext.pipeline.PresentRender(context);
		}
	}
}
