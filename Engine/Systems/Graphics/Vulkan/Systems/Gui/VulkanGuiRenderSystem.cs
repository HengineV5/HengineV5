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

		VkMeshBuffer meshBuffer;

		public VulkanGuiRenderSystem(VkContext context, VkRenderContext renderContext, IWindow window)
		{
			this.context = context;
			this.renderContext = renderContext;
			this.window = window;
		}

		public void Init()
		{
			Memory<GuiVertex> verticies = new GuiVertex[4];
			verticies.Span[0] = new(new(10, 0, 10, 0), new Vector2(0, 0));
			verticies.Span[1] = new(new(-10, 1, 10, 0), new Vector2(1, 0));
			verticies.Span[2] = new(new(10, 0, -10, 1), new Vector2(0, 1));
			verticies.Span[3] = new(new(-10, 1, -10, 1), new Vector2(1, 1));

			Memory<ushort> indicies = new ushort[6];
			indicies.Span[0] = 0;
			indicies.Span[1] = 3;
			indicies.Span[2] = 1;

			indicies.Span[3] = 0;
			indicies.Span[4] = 2;
			indicies.Span[5] = 3;

			meshBuffer = VulkanMeshResourceManager.CreateGuiMeshBuffer(context, verticies.Span, indicies.Span);
		}

		// TODO: Refactor out
		int bufferIdx;
		int updateIdx;

		float t = 0;

		[SystemPreLoop, SystemLayer(0, 2)]
		public void PreRenderPass()
		{
			//renderContext.pipeline.StartRender(this.context);
			renderContext.pipeline.StartRenderPass(context, RenderPassId.Gui, PipelineContainerLayer.Gui);

			bufferIdx = 0;
			updateIdx = 0;
		}

		[SystemUpdate, SystemLayer(0, 2)]
		public void BufferUpdate(ref VulkanRenderContext context, Position.Ref position, Size.Ref size)
		{
            ref GuiShaderInput shaderInput = ref renderContext.pipeline.GetUbo<GuiShaderInput>(bufferIdx);
			shaderInput.ubo.Value = context.guiUbo;
			shaderInput.ubo.Value.screenSize = new Vector2(window.Size.X, window.Size.Y);
            shaderInput.ubo.Value.proj = Matrix4x4.CreatePerspectiveFieldOfView(1.57f, 1, 0.1f, 100);
			//shaderInput.ubo.Value.view = Matrix4x4.Identity;
			//shaderInput.ubo.Value.translation = Matrix4x4.CreateTranslation(Vector3.UnitZ * t);

			t += 0.0001f;

            bufferIdx++;
		}

		[SystemUpdate, SystemLayer(0, 2)]
		public void RenderUpdate(ref VulkanRenderContext context, Position.Ref position, Size.Ref size)
		{
			renderContext.pipeline.Render(this.context, PipelineContainerLayer.Gui, meshBuffer.vertexBuffer, meshBuffer.indexBuffer, meshBuffer.indicies, updateIdx);
			updateIdx++;
		}

		[SystemPostLoop, SystemLayer(0, 2)]
		public void PostRenderPass()
		{
			renderContext.pipeline.EndRenderPass(context);
			//renderContext.pipeline.PresentRender(context);
		}
	}
}
