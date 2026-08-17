using RenderLib;

namespace Engine
{
	public struct HengineRenderGraph<TBackend> : IRenderGraph<HengineRenderGraph<TBackend>, TBackend, PipelineContainerLayer, RenderPassId>
		where TBackend : struct, IRenderBackend<TBackend>, allows ref struct
	{
		DescriptorSetContainer<TBackend> descriptorSets;
		PipelineContainer<TBackend> pipelines;
		RenderPassContainer<TBackend> renderPasses;

		public HengineRenderGraph(DescriptorSetContainer<TBackend> descriptorSets, PipelineContainer<TBackend> pipelines, RenderPassContainer<TBackend> renderPasses)
		{
			this.descriptorSets = descriptorSets;
			this.pipelines = pipelines;
			this.renderPasses = renderPasses;
		}

		public static HengineRenderGraph<TBackend> Create(ref TBackend backend)
		{
			var descriptorSets = DescriptorSetContainer<TBackend>.Create(ref backend);

			var (colorFormat, depthFormat) = TBackend.GetRenderPassFormats(ref backend);
			var renderPasses = RenderPassContainer<TBackend>.Create(ref backend, colorFormat, depthFormat);

			GpuRenderPass compatibleRenderPass = RenderPassContainer<TBackend>.GetCompatibleRenderPass(ref renderPasses);
			Extent2D extent = TBackend.GetRenderArea(ref backend).extent;

			var pipelines = PipelineContainer<TBackend>.Create(ref backend, compatibleRenderPass, extent, ref descriptorSets);

			return new HengineRenderGraph<TBackend>(descriptorSets, pipelines, renderPasses);
		}

		public static void Dispose(ref HengineRenderGraph<TBackend> self, ref TBackend backend)
		{
			RenderPassContainer<TBackend>.Dispose(ref backend, ref self.renderPasses);
			PipelineContainer<TBackend>.Dispose(ref backend, ref self.pipelines);
		}

		public static GpuRenderPass GetRenderPass(RenderPassId id, ref HengineRenderGraph<TBackend> self)
		{
			return RenderPassContainer<TBackend>.Get(id, ref self.renderPasses);
		}

		public static GpuRenderPass GetCompatibleRenderPass(ref HengineRenderGraph<TBackend> self)
		{
			return RenderPassContainer<TBackend>.GetCompatibleRenderPass(ref self.renderPasses);
		}

		public static GpuPipeline GetPipeline(PipelineContainerLayer layer, ref HengineRenderGraph<TBackend> self)
		{
			return PipelineContainer<TBackend>.Get(layer, ref self.pipelines);
		}

		public static GpuPipelineLayout GetPipelineLayout(PipelineContainerLayer layer, ref HengineRenderGraph<TBackend> self)
		{
			return PipelineContainer<TBackend>.GetLayout(layer, ref self.pipelines);
		}

		public static GpuDescriptorSet GetDescriptorSet(PipelineContainerLayer layer, uint frame, uint idx, ref HengineRenderGraph<TBackend> self)
		{
			return DescriptorSetContainer<TBackend>.GetDescriptorSet(layer, frame, idx, ref self.descriptorSets);
		}

		public static ref TUbo GetUbo<TUbo>(uint frame, uint idx) where TUbo : struct, IUniformBufferObject<TUbo, TBackend>
		{
			return ref DescriptorSetContainer<TBackend>.GetUbo<TUbo>(frame, idx);
		}

		public static ClearColor GetClearColor(ref HengineRenderGraph<TBackend> self)
		{
			System.Drawing.Color color = System.Drawing.Color.CornflowerBlue;
			return new ClearColor(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
		}
	}
}
