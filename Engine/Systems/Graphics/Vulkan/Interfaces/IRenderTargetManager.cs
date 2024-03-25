using Silk.NET.Vulkan;

namespace Engine
{
    public interface IRenderTargetManager<TSelf, TRenderPassInfo, TPipelineInfo>
		where TSelf : struct, IRenderTargetManager<TSelf, TRenderPassInfo, TPipelineInfo>
		where TRenderPassInfo : struct
		where TPipelineInfo : struct
	{
		static abstract TSelf Create(VkContext context, CommandPool commandPool);

		static abstract void Init(VkContext context, ref TSelf self, RenderPass compatibleRenderPass, CommandPool commandPool);

		static abstract void Dispose(VkContext context, ref TSelf self, CommandPool commandPool);

		static abstract RenderTarget AquireRenderTarget(VkContext context, ref TSelf self);

		static abstract bool PresentTarget(VkContext context, ref TSelf self, ref RenderTarget renderTarget);

		static abstract TRenderPassInfo GetRendePassInfo(VkContext context, ref TSelf self);

		static abstract TPipelineInfo GetPipelineInfo(VkContext context, ref TSelf self);

		static abstract Rect2D GetRenderArea(ref TSelf self);

		static abstract uint GetFramesInFlight();

		static abstract DescriptorSet GetDescriptorSet<TDescriptorContainer, TPipelineEnum>(TPipelineEnum layer, ref TDescriptorContainer container, uint idx, ref TSelf self)
			where TDescriptorContainer : struct, IDescriptorContainer<TDescriptorContainer, TPipelineEnum>
			where TPipelineEnum : Enum;

		static abstract ref TUbo GetUbo<TUbo, TDescriptorContainer, TPipelineEnum>(uint idx, scoped ref TSelf self)
			where TUbo : struct, IUniformBufferObject<TUbo>
			where TDescriptorContainer : struct, IDescriptorContainer<TDescriptorContainer, TPipelineEnum>
			where TPipelineEnum : Enum;
	}
}
