using EnCS.Attributes;
using Engine.Components;
using Engine.Graphics;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;

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
		public void BufferUpdate(ref VulkanRenderContext context, ref GuiProperties properties, ref GuiPosition position, ref VkTextBuffer text, ref VkTextureAtlas textureAtlas)
		{
			ref GuiShaderInput shaderInput = ref renderContext.pipeline.GetUbo<GuiShaderInput>(bufferIdx);
			shaderInput.ubo.Value = context.guiUbo;
			shaderInput.guiState.Value.bezier = true;

			shaderInput.ubo.Value.position = new Vector4f(position.x, position.y, position.z, position.w);
			shaderInput.ubo.Value.size = new Vector4f(100, 0, 100, 0);
			shaderInput.guiState.Value.totalStates = textureAtlas.textures;

			VulkanRenderHelpers.UpdateGuiDescriptorSet(this.context, renderContext.pipeline.GetDescriptorSet(PipelineContainerLayer.Gui, bufferIdx), textureAtlas.atlas, sampler);
		}

		[SystemUpdate, SystemLayer(0, 2)]
		public void RenderUpdate(ref VulkanRenderContext context, ref GuiProperties properties, ref GuiPosition position, ref VkTextBuffer text, ref VkTextureAtlas textureAtlas)
		{
			RenderMesh(text);
		}

		[SystemPostLoop, SystemLayer(0, 2)]
		public void PostRenderPass()
		{
			renderContext.pipeline.EndRenderPass(context);
		}

		int f = 0;
		int k = 0;
		float de = 0;

		void RenderMesh(VkTextBuffer textBuffer)
		{
			f++;
			if (f > 300)
			{
				f = 0;
				k++;

				if (k >= textBuffer.indexOffsets.Length - 1)
					k = 0;
			}

			de += Random.Shared.NextSingle() / 100f;

			string test = $"Hello, World! {de}";

			/*
			int idx = textBuffer.font.GetUnicodeGlyphIndex('7');
			//int idx = k;
			*/
			//int idx = 373;
			//int idx = 5;

			/*
			var start = textBuffer.indexOffsets.Span[idx];
			var stop = textBuffer.indexOffsets.Span[idx + 1];
			var length = stop - start;

			var vertStart = textBuffer.vertexOffsets.Span[idx];
			//length = 36;
			length = 30;

			Console.WriteLine($"Start: {start}, Length: {length}");

			Console.WriteLine($"Idx: {idx}");
			Console.WriteLine($"Length: {length}");
			Console.WriteLine($"Start: {start}");
			Console.WriteLine($"VertStart: {vertStart}");
			*/

			//var length = textBuffer.indexOffsets.Span[0];
			//Console.WriteLine(length);

			GuiPushConstant pushConstant = new();
			for (int i = 0; i < test.Length; i++)
			{
				int idx = textBuffer.font.GetUnicodeGlyphIndex(test[i]);

				var start = textBuffer.indexOffsets.Span[idx];
				var stop = textBuffer.indexOffsets.Span[idx + 1];
				var length = stop - start;

				renderContext.pipeline.Render(this.context, PipelineContainerLayer.Gui, textBuffer.vertexBuffer, textBuffer.indexBuffer, (uint)length, updateIdx, 0, (uint)start);

				pushConstant.offset += new Vector2f(textBuffer.advances.Span[idx], 0);
				renderContext.pipeline.SetPushConstant(this.context, PipelineContainerLayer.Gui, ref pushConstant);
			}

			pushConstant.offset = new Vector2f(0, 0);
			renderContext.pipeline.SetPushConstant(this.context, PipelineContainerLayer.Gui, ref pushConstant);

			//renderContext.pipeline.Render(this.context, PipelineContainerLayer.Gui, textBuffer.vertexBuffer, textBuffer.indexBuffer, textBuffer.indicies, updateIdx);
			//renderContext.pipeline.Render(this.context, PipelineContainerLayer.Gui, textBuffer.vertexBuffer, textBuffer.indexBuffer, (uint)length, updateIdx);
			//renderContext.pipeline.Render(this.context, PipelineContainerLayer.Gui, textBuffer.vertexBuffer, textBuffer.indexBuffer, (uint)length, updateIdx, 0, (uint)start);
		}
	}
}
