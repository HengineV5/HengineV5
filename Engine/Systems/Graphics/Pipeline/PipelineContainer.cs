using Engine.Graphics;
using RenderLib;

namespace Engine
{
	public struct GuiPushConstant
	{
		public Vector2f offset;
	}

	public struct RenderLayer
	{
		public GpuPipeline pipeline;
		public GpuPipelineLayout layout;

		public RenderLayer(GpuPipeline pipeline, GpuPipelineLayout layout)
		{
			this.pipeline = pipeline;
			this.layout = layout;
		}
	}

	public struct PipelineContainer<TBackend> where TBackend : struct, IRenderBackend<TBackend>, allows ref struct
	{
		public RenderLayer skyboxLayer;
		public RenderLayer pbrLayer;
		public RenderLayer wireframeLayer;
		public RenderLayer guiLayer;
		public RenderLayer gizmoLayer;
		public RenderLayer gizmoLineLayer;

		public PipelineContainer(RenderLayer skyboxLayer, RenderLayer pbrLayer, RenderLayer wireframeLayer, RenderLayer guiLayer, RenderLayer gizmoLayer, RenderLayer gizmoLineLayer)
		{
			this.skyboxLayer = skyboxLayer;
			this.pbrLayer = pbrLayer;
			this.wireframeLayer = wireframeLayer;
			this.guiLayer = guiLayer;
			this.gizmoLayer = gizmoLayer;
			this.gizmoLineLayer = gizmoLineLayer;
		}

		public unsafe static PipelineContainer<TBackend> Create(ref TBackend backend, GpuRenderPass compatibleRenderPass, Extent2D extent, ref DescriptorSetContainer<TBackend> descriptorSets)
		{
			var pushConstant = new PushConstantRange(0, (uint)sizeof(GuiPushConstant), ShaderStage.Vertex);

			var pbrLayout = TBackend.CreatePipelineLayout(ref backend, new PipelineLayoutDesc(
				DescriptorSetContainer<TBackend>.GetDescriptorSetLayout(PipelineContainerLayer.Pbr, ref descriptorSets), pushConstant));
			var guiLayout = TBackend.CreatePipelineLayout(ref backend, new PipelineLayoutDesc(
				DescriptorSetContainer<TBackend>.GetDescriptorSetLayout(PipelineContainerLayer.Gui, ref descriptorSets), pushConstant));
			var gizmoLayout = TBackend.CreatePipelineLayout(ref backend, new PipelineLayoutDesc(
				DescriptorSetContainer<TBackend>.GetDescriptorSetLayout(PipelineContainerLayer.Gizmo, ref descriptorSets), pushConstant));

			var skyboxShader = ShaderSource.FromFiles("Shaders/Skybox/SkyboxVert.spv", "Shaders/Skybox/SkyboxFrag.spv");
			var skybox = CreateLayer(ref backend, skyboxShader, Vertex.Layout, extent, PolygonMode.Fill, CullMode.Front, 1, true, pbrLayout, compatibleRenderPass);

			var pbrShader = ShaderSource.FromFiles("Shaders/Pbr/PbrVert.spv", "Shaders/Pbr/PbrFrag.spv");
			var pbr = CreateLayer(ref backend, pbrShader, Vertex.Layout, extent, PolygonMode.Fill, CullMode.Back, 1, true, pbrLayout, compatibleRenderPass);

			var wireframeShader = ShaderSource.FromFiles("Shaders/Pbr/PbrVert.spv", "Shaders/Pbr/BlackFrag.spv");
			var wireframe = CreateLayer(ref backend, wireframeShader, Vertex.Layout, extent, PolygonMode.Line, CullMode.None, 1, true, pbrLayout, compatibleRenderPass);

			var guiShader = ShaderSource.FromFiles("Shaders/Gui/GuiVert.spv", "Shaders/Gui/GuiFrag.spv");
			var gui = CreateLayer(ref backend, guiShader, GuiVertex.Layout, extent, PolygonMode.Fill, CullMode.None, 1, false, guiLayout, compatibleRenderPass);

			var gizmoShader = ShaderSource.FromFiles("Shaders/Gizmo/GizmoVert.spv", "Shaders/Gizmo/GizmoFrag.spv");
			var gizmo = CreateLayer(ref backend, gizmoShader, GizmoVertex.Layout, extent, PolygonMode.Fill, CullMode.Back, 1, false, gizmoLayout, compatibleRenderPass);
			var gizmoLine = CreateLayer(ref backend, gizmoShader, GizmoVertex.Layout, extent, PolygonMode.Line, CullMode.None, 2, false, gizmoLayout, compatibleRenderPass);

			return new PipelineContainer<TBackend>(skybox, pbr, wireframe, gui, gizmo, gizmoLine);
		}

		public static void Dispose(ref TBackend backend, ref PipelineContainer<TBackend> self)
		{
			TBackend.DestroyPipeline(ref backend, self.skyboxLayer.pipeline);
			TBackend.DestroyPipeline(ref backend, self.pbrLayer.pipeline);
			TBackend.DestroyPipeline(ref backend, self.wireframeLayer.pipeline);
			TBackend.DestroyPipeline(ref backend, self.guiLayer.pipeline);
			TBackend.DestroyPipeline(ref backend, self.gizmoLayer.pipeline);
			TBackend.DestroyPipeline(ref backend, self.gizmoLineLayer.pipeline);

			// Skybox, pbr and wireframe share a layout, as do gizmo and gizmoLine
			TBackend.DestroyPipelineLayout(ref backend, self.skyboxLayer.layout);
			TBackend.DestroyPipelineLayout(ref backend, self.guiLayer.layout);
			TBackend.DestroyPipelineLayout(ref backend, self.gizmoLayer.layout);
		}

		public static GpuPipeline Get(PipelineContainerLayer layer, ref PipelineContainer<TBackend> self)
		{
			return GetLayer(layer, ref self).pipeline;
		}

		public static GpuPipelineLayout GetLayout(PipelineContainerLayer layer, ref PipelineContainer<TBackend> self)
		{
			return GetLayer(layer, ref self).layout;
		}

		static ref RenderLayer GetLayer(PipelineContainerLayer layer, ref PipelineContainer<TBackend> self)
		{
			switch (layer)
			{
				case PipelineContainerLayer.Skybox:
					return ref self.skyboxLayer;
				case PipelineContainerLayer.Pbr:
					return ref self.pbrLayer;
				case PipelineContainerLayer.Wireframe:
					return ref self.wireframeLayer;
				case PipelineContainerLayer.Gui:
					return ref self.guiLayer;
				case PipelineContainerLayer.Gizmo:
					return ref self.gizmoLayer;
				case PipelineContainerLayer.GizmoLine:
					return ref self.gizmoLineLayer;
				default:
					throw new Exception();
			}
		}

		static RenderLayer CreateLayer(ref TBackend backend, ShaderSource shader, VertexLayout vertexLayout, Extent2D extent, PolygonMode polygonMode, CullMode cullMode, float lineWidth, bool depthTest, GpuPipelineLayout layout, GpuRenderPass compatibleRenderPass)
		{
			var desc = new PipelineDesc(shader, vertexLayout, extent, PrimitiveTopology.TriangleList, polygonMode, cullMode, lineWidth, depthTest, true);

			return new RenderLayer(TBackend.CreatePipeline(ref backend, desc, layout, compatibleRenderPass), layout);
		}
	}
}
