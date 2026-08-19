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

	public struct PipelineContainer<TStorage> where TStorage : struct, IRenderBackend<TStorage>, allows ref struct
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

		public unsafe static PipelineContainer<TStorage> Create(ref TStorage storage, GpuRenderPass compatibleRenderPass, Extent2D extent, ref DescriptorSetContainer<TStorage> descriptorSets)
		{
			var pushConstant = new PushConstantRange(0, (uint)sizeof(GuiPushConstant), ShaderStage.Vertex);

			var pbrLayout = TStorage.CreatePipelineLayout(ref storage, new PipelineLayoutDesc(
				DescriptorSetContainer<TStorage>.GetDescriptorSetLayout(PipelineContainerLayer.Pbr, ref descriptorSets), pushConstant));
			var guiLayout = TStorage.CreatePipelineLayout(ref storage, new PipelineLayoutDesc(
				DescriptorSetContainer<TStorage>.GetDescriptorSetLayout(PipelineContainerLayer.Gui, ref descriptorSets), pushConstant));
			var gizmoLayout = TStorage.CreatePipelineLayout(ref storage, new PipelineLayoutDesc(
				DescriptorSetContainer<TStorage>.GetDescriptorSetLayout(PipelineContainerLayer.Gizmo, ref descriptorSets), pushConstant));

			var skyboxShader = ShaderSource.FromFiles("Shaders/Skybox/SkyboxVert.spv", "Shaders/Skybox/SkyboxFrag.spv");
			var skybox = CreateLayer(ref storage, skyboxShader, Vertex.Layout, extent, PolygonMode.Fill, CullMode.None, 1, true, pbrLayout, compatibleRenderPass);

			var pbrShader = ShaderSource.FromFiles("Shaders/Pbr/PbrVert.spv", "Shaders/Pbr/PbrFrag.spv");
			var pbr = CreateLayer(ref storage, pbrShader, Vertex.Layout, extent, PolygonMode.Fill, CullMode.Back, 1, true, pbrLayout, compatibleRenderPass);

			var wireframeShader = ShaderSource.FromFiles("Shaders/Pbr/PbrVert.spv", "Shaders/Pbr/BlackFrag.spv");
			var wireframe = CreateLayer(ref storage, wireframeShader, Vertex.Layout, extent, PolygonMode.Line, CullMode.None, 1, true, pbrLayout, compatibleRenderPass);

			var guiShader = ShaderSource.FromFiles("Shaders/Gui/GuiVert.spv", "Shaders/Gui/GuiFrag.spv");
			var gui = CreateLayer(ref storage, guiShader, GuiVertex.Layout, extent, PolygonMode.Fill, CullMode.None, 1, false, guiLayout, compatibleRenderPass);

			var gizmoShader = ShaderSource.FromFiles("Shaders/Gizmo/GizmoVert.spv", "Shaders/Gizmo/GizmoFrag.spv");
			var gizmo = CreateLayer(ref storage, gizmoShader, GizmoVertex.Layout, extent, PolygonMode.Fill, CullMode.Back, 1, false, gizmoLayout, compatibleRenderPass);
			var gizmoLine = CreateLayer(ref storage, gizmoShader, GizmoVertex.Layout, extent, PolygonMode.Line, CullMode.None, 2, false, gizmoLayout, compatibleRenderPass);

			return new PipelineContainer<TStorage>(skybox, pbr, wireframe, gui, gizmo, gizmoLine);
		}

		public static void Dispose(ref TStorage storage, ref PipelineContainer<TStorage> self)
		{
			TStorage.DestroyPipeline(ref storage, self.skyboxLayer.pipeline);
			TStorage.DestroyPipeline(ref storage, self.pbrLayer.pipeline);
			TStorage.DestroyPipeline(ref storage, self.wireframeLayer.pipeline);
			TStorage.DestroyPipeline(ref storage, self.guiLayer.pipeline);
			TStorage.DestroyPipeline(ref storage, self.gizmoLayer.pipeline);
			TStorage.DestroyPipeline(ref storage, self.gizmoLineLayer.pipeline);

			// Skybox, pbr and wireframe share a layout, as do gizmo and gizmoLine
			TStorage.DestroyPipelineLayout(ref storage, self.skyboxLayer.layout);
			TStorage.DestroyPipelineLayout(ref storage, self.guiLayer.layout);
			TStorage.DestroyPipelineLayout(ref storage, self.gizmoLayer.layout);
		}

		public static GpuPipeline Get(PipelineContainerLayer layer, ref PipelineContainer<TStorage> self)
		{
			return GetLayer(layer, ref self).pipeline;
		}

		public static GpuPipelineLayout GetLayout(PipelineContainerLayer layer, ref PipelineContainer<TStorage> self)
		{
			return GetLayer(layer, ref self).layout;
		}

		static ref RenderLayer GetLayer(PipelineContainerLayer layer, ref PipelineContainer<TStorage> self)
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

		static RenderLayer CreateLayer(ref TStorage storage, ShaderSource shader, VertexLayout vertexLayout, Extent2D extent, PolygonMode polygonMode, CullMode cullMode, float lineWidth, bool depthTest, GpuPipelineLayout layout, GpuRenderPass compatibleRenderPass)
		{
			var desc = new PipelineDesc(shader, vertexLayout, extent, PrimitiveTopology.TriangleList, polygonMode, cullMode, lineWidth, depthTest, true);

			return new RenderLayer(TStorage.CreatePipeline(ref storage, desc, layout, compatibleRenderPass), layout);
		}
	}
}
