using Silk.NET.Vulkan;

namespace Engine
{
    public interface IPipelineContainer<TSelf, TPipelineInfo, TEnum>
		where TSelf : IPipelineContainer<TSelf, TPipelineInfo, TEnum>
		where TPipelineInfo : struct
		where TEnum : Enum
    {
        static abstract TSelf Create<TDescriptorContainer>(VkContext context, RenderPass compatibleRenderPass, in TPipelineInfo pipelineInfo)
            where TDescriptorContainer : struct, IDescriptorContainer<TDescriptorContainer, TEnum>;

        static abstract void Dispose(VkContext context, ref TSelf self);

        static abstract Pipeline Get(TEnum mask, ref TSelf self);

        static abstract PipelineLayout GetLayout(TEnum mask, ref TSelf self);
    }
}
