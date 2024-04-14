using EnCS;
using EnCS.Attributes;
using Engine.Components;
using Engine.Graphics;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using System.Numerics;

namespace Engine
{
	[System<VulkanRenderContext>]
	[UsingResource<VulkanTextureAtlasResourceManager>]
	public partial class VulkanGuiRenderSystem
	{
		IWindow window;
		IInputHandler inputHandler;

		VkContext context;
		VkRenderContext renderContext;

		VkMeshBuffer meshBuffer;
		Sampler sampler;

		public VulkanGuiRenderSystem(VkContext context, VkRenderContext renderContext, IWindow window, IInputHandler inputHandler)
		{
			this.context = context;
			this.renderContext = renderContext;
			this.window = window;
			this.inputHandler = inputHandler;
		}

		public void Init()
		{
			sampler = VulkanHelper.CreateSampler(context, 5);

			float borderSize = 20;

			Memory<GuiVertex> verticies = new GuiVertex[16];
			verticies.Span[0] = new(new(0, 0, 0, 0),			new Vector2(0, 0));
			verticies.Span[1] = new(new(0, 0, 0, borderSize),	new Vector2(0, 0.33f));

			verticies.Span[2] = new(new(0, 0, 1, -borderSize),	new Vector2(0, 0.66f));
			verticies.Span[3] = new(new(0, 0, 1, 0),			new Vector2(0, 1));
			verticies.Span[4] = new(new(0, borderSize, 1, 0),	new Vector2(0.33f, 1));

			verticies.Span[5] = new(new(1, -borderSize, 1, 0),	new Vector2(0.77f, 1));
			verticies.Span[6] = new(new(1, 0, 1, 0),			new Vector2(1, 1));
			verticies.Span[7] = new(new(1, 0, 1, -borderSize),	new Vector2(1, 0.77f));

			verticies.Span[8] = new(new(1, 0, 0, borderSize),	new Vector2(1, 0.33f));
			verticies.Span[9] = new(new(1, 0, 0, 0),			new Vector2(1, 0));
			verticies.Span[10] = new(new(1, -borderSize, 0, 0), new Vector2(0.77f, 0));

			verticies.Span[11] = new(new(0, borderSize, 0, 0),	new Vector2(0.33f, 0));

			verticies.Span[12] = new(new(0, borderSize, 0, borderSize), new Vector2(0.33f, 0.33f));
			verticies.Span[13] = new(new(0, borderSize, 1, -borderSize), new Vector2(0.33f, 0.66f));
			verticies.Span[14] = new(new(1, -borderSize, 1, -borderSize), new Vector2(0.66f, 0.66f));
			verticies.Span[15] = new(new(1, -borderSize, 0, borderSize), new Vector2(0.66f, 0.33f));

			Memory<ushort> indicies = new ushort[54];
			indicies.Span[0] = 0;
			indicies.Span[1] = 1;
			indicies.Span[2] = 12;

			indicies.Span[3] = 0;
			indicies.Span[4] = 12;
			indicies.Span[5] = 11;

			indicies.Span[6] = 1;
			indicies.Span[7] = 2;
			indicies.Span[8] = 13;

			indicies.Span[9] = 1;
			indicies.Span[10] = 13;
			indicies.Span[11] = 12;

			indicies.Span[12] = 2;
			indicies.Span[13] = 3;
			indicies.Span[14] = 4;

			indicies.Span[15] = 2;
			indicies.Span[16] = 4;
			indicies.Span[17] = 13;

			indicies.Span[18] = 13;
			indicies.Span[19] = 4;
			indicies.Span[20] = 5;

			indicies.Span[21] = 13;
			indicies.Span[22] = 5;
			indicies.Span[23] = 14;

			indicies.Span[24] = 14;
			indicies.Span[25] = 5;
			indicies.Span[26] = 6;

			indicies.Span[27] = 14;
			indicies.Span[28] = 6;
			indicies.Span[29] = 7;

			indicies.Span[30] = 14;
			indicies.Span[31] = 7;
			indicies.Span[32] = 8;

			indicies.Span[33] = 14;
			indicies.Span[34] = 8;
			indicies.Span[35] = 15;

			indicies.Span[36] = 15;
			indicies.Span[37] = 8;
			indicies.Span[38] = 9;

			indicies.Span[39] = 15;
			indicies.Span[40] = 9;
			indicies.Span[41] = 10;

			indicies.Span[42] = 15;
			indicies.Span[43] = 10;
			indicies.Span[44] = 11;

			indicies.Span[45] = 15;
			indicies.Span[46] = 11;
			indicies.Span[47] = 12;

			indicies.Span[48] = 12;
			indicies.Span[49] = 14;
			indicies.Span[50] = 15;

			indicies.Span[51] = 12;
			indicies.Span[52] = 13;
			indicies.Span[53] = 14;

			meshBuffer = VulkanMeshResourceManager.CreateGuiMeshBuffer(context, verticies.Span, indicies.Span);
		}

		// TODO: Refactor out
		int bufferIdx;
		int updateIdx;

		float t = 0;

		[SystemPreLoop, SystemLayer(0, 2)]
		public void PreRenderPass()
		{
			renderContext.pipeline.StartRenderPass(context, RenderPassId.Gui, PipelineContainerLayer.Gui);

			bufferIdx = 0;
			updateIdx = 0;
		}

		[SystemUpdate, SystemLayer(0, 2)]
		public void BufferUpdate(ref VulkanRenderContext context, GuiPosition.Ref position, GuiSize.Ref size, ref VkTextureAtlas textureAtlas)
		{
            ref GuiShaderInput shaderInput = ref renderContext.pipeline.GetUbo<GuiShaderInput>(bufferIdx);
			shaderInput.ubo.Value = context.guiUbo;
			shaderInput.ubo.Value.screenSize = new Vector2(window.Size.X, window.Size.Y);
            shaderInput.ubo.Value.proj = Matrix4x4.CreatePerspectiveFieldOfView(1.57f, 1, 0.1f, 100);
			shaderInput.ubo.Value.position = new Vector4(position.x, position.y, position.z, position.w);
			shaderInput.ubo.Value.size = new Vector4(size.x, size.y, size.z, size.w);
			shaderInput.guiState.Value.totalStates = textureAtlas.textures;

			Vector2 p1 = new Vector2(shaderInput.ubo.Value.position.X + shaderInput.ubo.Value.position.Y * window.Size.X, shaderInput.ubo.Value.position.Z + shaderInput.ubo.Value.position.W * window.Size.X);
			Vector2 p2 = p1 + new Vector2(shaderInput.ubo.Value.size.X + shaderInput.ubo.Value.size.Y * window.Size.X, shaderInput.ubo.Value.size.Z + shaderInput.ubo.Value.size.W * window.Size.X);

			Vector2 mousePos = inputHandler.GetMousePosition();
            
			if (mousePos.X > p1.X && mousePos.X < p2.X && mousePos.Y > p1.Y && mousePos.Y < p2.Y)
			{
				shaderInput.guiState.Value.state = inputHandler.IsKeyDown(Silk.NET.Input.MouseButton.Left) ? 2 : 1;
			}
			else
				shaderInput.guiState.Value.state = 0;

            VulkanRenderHelpers.UpdateGuiDescriptorSet(this.context, renderContext.pipeline.GetDescriptorSet(PipelineContainerLayer.Gui, bufferIdx), textureAtlas.atlas, sampler);

			t += 0.0001f;

            bufferIdx++;
		}

		[SystemUpdate, SystemLayer(0, 2)]
		public void RenderUpdate(ref VulkanRenderContext context, GuiPosition.Ref position, GuiSize.Ref size, ref VkTextureAtlas textureAtlas)
		{
			renderContext.pipeline.Render(this.context, PipelineContainerLayer.Gui, meshBuffer.vertexBuffer, meshBuffer.indexBuffer, meshBuffer.indicies, updateIdx);
			updateIdx++;
		}

		[SystemPostLoop, SystemLayer(0, 2)]
		public void PostRenderPass()
		{
			renderContext.pipeline.EndRenderPass(context);
		}
	}
}
