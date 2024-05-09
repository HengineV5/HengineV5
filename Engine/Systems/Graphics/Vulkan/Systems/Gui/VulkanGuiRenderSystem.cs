using EnCS;
using EnCS.Attributes;
using Engine.Components;
using Engine.Graphics;
using Engine.Parsing;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using System.Numerics;

namespace Engine
{
	[System]
	[SystemContext<VulkanRenderContext>]
	[UsingResource<VulkanTextureAtlasResourceManager>]
	public partial class VulkanGuiRenderSystem
	{
		IWindow window;
		IInputHandler inputHandler;

		VkContext context;
		VkRenderContext renderContext;

		VkMeshBuffer boxBuffer;
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

			var boxMesh = GuiMeshes.Glyph;
			boxBuffer = VulkanMeshResourceManager.CreateGuiMeshBuffer(context, boxMesh.verticies, boxMesh.indicies);
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
		public void BufferUpdate(ref VulkanRenderContext context, GuiProperties.Ref properties, GuiPosition.Ref position, GuiSize.Ref size, ref VkTextureAtlas textureAtlas)
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
		public void RenderUpdate(ref VulkanRenderContext context, GuiProperties.Ref properties, GuiPosition.Ref position, GuiSize.Ref size, ref VkTextureAtlas textureAtlas)
		{
			switch(properties.shape)
			{
				case GuiShape.Box:
					RenderMesh(boxBuffer);
					break;
				default:
					throw new Exception("Shape not supported");
			}

			updateIdx++;
		}

		[SystemPostLoop, SystemLayer(0, 2)]
		public void PostRenderPass()
		{
			renderContext.pipeline.EndRenderPass(context);
		}

		void RenderMesh(VkMeshBuffer meshBuffer)
		{
			renderContext.pipeline.Render(this.context, PipelineContainerLayer.Gui, meshBuffer.vertexBuffer, meshBuffer.indexBuffer, meshBuffer.indicies, updateIdx);
		}
	}

	public static class GuiMeshes
	{
		public static GuiMesh Box => BoxMesh();

		public static GuiMesh Glyph => GlyphMesh();

		static GuiMesh BoxMesh()
		{
			float borderSize = 1;

			GuiVertex[] verticies = new GuiVertex[16];
			verticies[0] = new(new(0, 0, 0, 0), new Vector2(0, 0));
			verticies[1] = new(new(0, 0, 0, borderSize), new Vector2(0, 0.33f));

			verticies[2] = new(new(0, 0, 1, -borderSize), new Vector2(0, 0.66f));
			verticies[3] = new(new(0, 0, 1, 0), new Vector2(0, 1));
			verticies[4] = new(new(0, borderSize, 1, 0), new Vector2(0.33f, 1));

			verticies[5] = new(new(1, -borderSize, 1, 0), new Vector2(0.77f, 1));
			verticies[6] = new(new(1, 0, 1, 0), new Vector2(1, 1));
			verticies[7] = new(new(1, 0, 1, -borderSize), new Vector2(1, 0.77f));

			verticies[8] = new(new(1, 0, 0, borderSize), new Vector2(1, 0.33f));
			verticies[9] = new(new(1, 0, 0, 0), new Vector2(1, 0));
			verticies[10] = new(new(1, -borderSize, 0, 0), new Vector2(0.77f, 0));

			verticies[11] = new(new(0, borderSize, 0, 0), new Vector2(0.33f, 0));

			verticies[12] = new(new(0, borderSize, 0, borderSize), new Vector2(0.33f, 0.33f));
			verticies[13] = new(new(0, borderSize, 1, -borderSize), new Vector2(0.33f, 0.66f));
			verticies[14] = new(new(1, -borderSize, 1, -borderSize), new Vector2(0.66f, 0.66f));
			verticies[15] = new(new(1, -borderSize, 0, borderSize), new Vector2(0.66f, 0.33f));

			ushort[] indicies = new ushort[54];
			indicies[0] = 0;
			indicies[1] = 1;
			indicies[2] = 12;

			indicies[3] = 0;
			indicies[4] = 12;
			indicies[5] = 11;

			indicies[6] = 1;
			indicies[7] = 2;
			indicies[8] = 13;

			indicies[9] = 1;
			indicies[10] = 13;
			indicies[11] = 12;

			indicies[12] = 2;
			indicies[13] = 3;
			indicies[14] = 4;

			indicies[15] = 2;
			indicies[16] = 4;
			indicies[17] = 13;

			indicies[18] = 13;
			indicies[19] = 4;
			indicies[20] = 5;

			indicies[21] = 13;
			indicies[22] = 5;
			indicies[23] = 14;

			indicies[24] = 14;
			indicies[25] = 5;
			indicies[26] = 6;

			indicies[27] = 14;
			indicies[28] = 6;
			indicies[29] = 7;

			indicies[30] = 14;
			indicies[31] = 7;
			indicies[32] = 8;

			indicies[33] = 14;
			indicies[34] = 8;
			indicies[35] = 15;

			indicies[36] = 15;
			indicies[37] = 8;
			indicies[38] = 9;

			indicies[39] = 15;
			indicies[40] = 9;
			indicies[41] = 10;

			indicies[42] = 15;
			indicies[43] = 10;
			indicies[44] = 11;

			indicies[45] = 15;
			indicies[46] = 11;
			indicies[47] = 12;

			indicies[48] = 12;
			indicies[49] = 14;
			indicies[50] = 15;

			indicies[51] = 12;
			indicies[52] = 13;
			indicies[53] = 14;

			return new GuiMesh()
			{
				verticies = verticies,
				indicies = indicies
			};
		}

		static GuiMesh GlyphMesh()
		{
			var font = TtfLoader.LoadFont("Fonts/arial.ttf");

			var g = font.GetGlyphIndex('H');
			Vector2[] coords = new Vector2[g.xCoords.Length];
			Vector2 delta = new Vector2(g.glyphDescription.xMax - g.glyphDescription.xMin, g.glyphDescription.yMax - g.glyphDescription.yMin);
			for (int i = 0; i < coords.Length; i++)
			{
				coords[i] = new Vector2(g.xCoords[i] / delta.X, g.yCoords[i] / delta.Y);
				Console.WriteLine(coords[i]);
            }

			var indicies = Triangulation.Triangulate(coords.AsSpan().Slice(0, g.endPtsOfContours[0]));
			/*
			for (int i = 0; i < indicies.Length; i++)
			{
				if (i != 0 && i % 3 == 0)
					Console.WriteLine();

				Console.WriteLine(indicies.Span[i]);
			}
			*/

			GuiMesh guiMesh = new GuiMesh();
			guiMesh.verticies = coords.Select(x => new GuiVertex(new Vector4(x.X, 0, x.Y, 0), Vector2.Zero)).ToArray();
			guiMesh.indicies = indicies.Span.ToArray().Select(x => (ushort)x).ToArray();

			return guiMesh;
		}

		/*
		static GuiMesh CircleMesh()
		{
			GuiVertex[] verticies = new GuiVertex[32];

			float stepSize = MathF.PI * 2 / verticies.Length;	
            for (int i = 0; i < verticies.Length; i++)
            {
				verticies[i] = new GuiVertex(new(0, MathF.Cos(i * stepSize), 0, MathF.Sin(i * stepSize)), new(0.5f, 0.5f));
            }


        }
		*/
	}
}
