using RenderLib;

namespace Engine
{
	public struct HengineRenderGraph<TStorage> : IRenderGraph<HengineRenderGraph<TStorage>, TStorage, PipelineContainerLayer, RenderPassId>
		where TStorage : struct, IRenderBackend<TStorage>, allows ref struct
	{
		DescriptorSetContainer<TStorage> descriptorSets;
		PipelineContainer<TStorage> pipelines;
		RenderPassContainer<TStorage> renderPasses;

		public HengineRenderGraph(DescriptorSetContainer<TStorage> descriptorSets, PipelineContainer<TStorage> pipelines, RenderPassContainer<TStorage> renderPasses)
		{
			this.descriptorSets = descriptorSets;
			this.pipelines = pipelines;
			this.renderPasses = renderPasses;
		}

		public static HengineRenderGraph<TStorage> Create(ref TStorage storage)
		{
			var descriptorSets = DescriptorSetContainer<TStorage>.Create(ref storage);

			var (colorFormat, depthFormat) = TStorage.GetRenderPassFormats(ref storage);
			var renderPasses = RenderPassContainer<TStorage>.Create(ref storage, colorFormat, depthFormat);

			GpuRenderPass compatibleRenderPass = RenderPassContainer<TStorage>.GetCompatibleRenderPass(ref renderPasses);
			Extent2D extent = TStorage.GetRenderArea(ref storage).extent;

			var pipelines = PipelineContainer<TStorage>.Create(ref storage, compatibleRenderPass, extent, ref descriptorSets);

			return new HengineRenderGraph<TStorage>(descriptorSets, pipelines, renderPasses);
		}

		public static void Dispose(ref HengineRenderGraph<TStorage> self, ref TStorage storage)
		{
			RenderPassContainer<TStorage>.Dispose(ref storage, ref self.renderPasses);
			PipelineContainer<TStorage>.Dispose(ref storage, ref self.pipelines);
		}

		public static GpuRenderPass GetRenderPass(RenderPassId id, ref HengineRenderGraph<TStorage> self)
		{
			return RenderPassContainer<TStorage>.Get(id, ref self.renderPasses);
		}

		public static GpuRenderPass GetCompatibleRenderPass(ref HengineRenderGraph<TStorage> self)
		{
			return RenderPassContainer<TStorage>.GetCompatibleRenderPass(ref self.renderPasses);
		}

		public static GpuPipeline GetPipeline(PipelineContainerLayer layer, ref HengineRenderGraph<TStorage> self)
		{
			return PipelineContainer<TStorage>.Get(layer, ref self.pipelines);
		}

		public static GpuPipelineLayout GetPipelineLayout(PipelineContainerLayer layer, ref HengineRenderGraph<TStorage> self)
		{
			return PipelineContainer<TStorage>.GetLayout(layer, ref self.pipelines);
		}

		public static GpuDescriptorSet GetDescriptorSet(PipelineContainerLayer layer, uint frame, uint idx, ref HengineRenderGraph<TStorage> self)
		{
			return DescriptorSetContainer<TStorage>.GetDescriptorSet(layer, frame, idx, ref self.descriptorSets);
		}

		public static ref TUbo GetUbo<TUbo>(uint frame, uint idx) where TUbo : struct, IUniformBufferObject<TUbo, TStorage>
		{
			return ref DescriptorSetContainer<TStorage>.GetUbo<TUbo>(frame, idx);
		}

		public static ClearColor GetClearColor(ref HengineRenderGraph<TStorage> self)
		{
			System.Drawing.Color color = System.Drawing.Color.CornflowerBlue;
			return new ClearColor(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
		}
	}
}
