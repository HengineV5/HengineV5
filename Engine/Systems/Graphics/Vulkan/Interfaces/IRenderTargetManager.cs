using Silk.NET.Vulkan;

namespace Engine
{
    public interface IRenderTargetManager<TSelf, TDescriptorSet, TRenderPassInfo, TPipelineInfo>
		where TSelf : struct, IRenderTargetManager<TSelf, TDescriptorSet, TRenderPassInfo, TPipelineInfo>
		where TDescriptorSet : struct, IDescriptorSet<TDescriptorSet>
		where TRenderPassInfo : struct
		where TPipelineInfo : struct
	{
		static abstract TSelf Create(VkContext context, CommandPool commandPool);

		static abstract void Init(VkContext context, ref TSelf self, RenderPass compatibleRenderPass, CommandPool commandPool);

		static abstract void Dispose(VkContext context, ref TSelf self, CommandPool commandPool);

		static abstract RenderTarget<TDescriptorSet> AquireRenderTarget(VkContext context, ref TSelf self);

		static abstract bool PresentTarget(VkContext context, ref TSelf self, ref RenderTarget<TDescriptorSet> renderTarget);

		static abstract TRenderPassInfo GetRendePassInfo(VkContext context, ref TSelf self);

		static abstract TPipelineInfo GetPipelineInfo(VkContext context, ref TSelf self);

		static abstract Rect2D GetRenderArea(ref TSelf self);
	}
}
