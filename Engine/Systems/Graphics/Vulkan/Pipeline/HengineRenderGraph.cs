using RenderLib;
using RenderLib.Vulkan;

using Backend = RenderLib.Backend;

namespace Engine
{
	public struct HengineRenderGraph : IRenderGraph<HengineRenderGraph, Backend.Vulkan, PipelineContainerLayer, RenderPassId>
	{
		DescriptorSetContainer descriptorSets;
		PipelineContainer pipelines;
		RenderPassContainer renderPasses;

		public HengineRenderGraph(DescriptorSetContainer descriptorSets, PipelineContainer pipelines, RenderPassContainer renderPasses)
		{
			this.descriptorSets = descriptorSets;
			this.pipelines = pipelines;
			this.renderPasses = renderPasses;
		}

		public static HengineRenderGraph Create(ref Backend.Vulkan backend)
		{
			VkContext context = backend.Context;

			var descriptorSets = DescriptorSetContainer.Create(ref backend);
			var renderPasses = RenderPassContainer.Create(context, Backend.Vulkan.GetRenderPassInfo(ref backend));

			GpuRenderPass compatibleRenderPass = RenderPassContainer.GetCompatibleRenderPass(ref renderPasses);
			var pipelines = PipelineContainer.Create(context, compatibleRenderPass, Backend.Vulkan.GetPipelineInfo(ref backend));

			return new HengineRenderGraph(descriptorSets, pipelines, renderPasses);
		}

		public static void Dispose(ref HengineRenderGraph self, ref Backend.Vulkan backend)
		{
			VkContext context = backend.Context;

			RenderPassContainer.Dispose(context, ref self.renderPasses);
			PipelineContainer.Dispose(context, ref self.pipelines);
		}

		public static GpuRenderPass GetRenderPass(RenderPassId id, ref HengineRenderGraph self)
		{
			return RenderPassContainer.Get(id, ref self.renderPasses);
		}

		public static GpuRenderPass GetCompatibleRenderPass(ref HengineRenderGraph self)
		{
			return RenderPassContainer.GetCompatibleRenderPass(ref self.renderPasses);
		}

		public static GpuPipeline GetPipeline(PipelineContainerLayer layer, ref HengineRenderGraph self)
		{
			return PipelineContainer.Get(layer, ref self.pipelines);
		}

		public static GpuPipelineLayout GetPipelineLayout(PipelineContainerLayer layer, ref HengineRenderGraph self)
		{
			return PipelineContainer.GetLayout(layer, ref self.pipelines);
		}

		public static GpuDescriptorSet GetDescriptorSet(PipelineContainerLayer layer, uint frame, uint idx, ref HengineRenderGraph self)
		{
			return DescriptorSetContainer.GetDescriptorSet(layer, frame, idx, ref self.descriptorSets);
		}

		public static ref TUbo GetUbo<TUbo>(uint frame, uint idx) where TUbo : struct, IUniformBufferObject<TUbo, Backend.Vulkan>
		{
			return ref DescriptorSetContainer.GetUbo<TUbo>(frame, idx);
		}

		public static ClearColor GetClearColor(ref HengineRenderGraph self)
		{
			System.Drawing.Color color = System.Drawing.Color.CornflowerBlue;
			return new ClearColor(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
		}
	}
}
