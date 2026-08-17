using EnCS.Attributes;
using Engine.Graphics;
using Silk.NET.Windowing;

using RenderLib;

namespace Engine
{
	[System]
	[SystemContext<RenderContext>]
	public partial class PresentSystem
	{
		IWindow window;
		GraphicsContext renderContext;

		public PresentSystem(GraphicsContext renderContext, IWindow window)
		{
			this.renderContext = renderContext;
			this.window = window;
		}

		[SystemUpdate]
		public void UpdateCamera(ref RenderContext context, ref Camera camera)
		{
		}

		public void PostRun()
		{
			var backend = renderContext.CreateBackend();

			renderContext.pipeline.PresentRender(ref backend);
		}
	}
}
