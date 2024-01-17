using Silk.NET.Vulkan;

namespace Engine
{
    public interface IPipelineContainer<TSelf, TPipelineInfo, TEnum>
		where TSelf : IPipelineContainer<TSelf, TPipelineInfo, TEnum>
		where TPipelineInfo : struct
		where TEnum : Enum
    {
        static abstract TSelf Create(VkContext context, DescriptorSetLayout descriptorSetLayout, in TPipelineInfo pipelineInfo);

        static abstract Pipeline Get(TEnum mask, ref TSelf self);

        static abstract PipelineLayout GetLayout(TEnum mask, ref TSelf self);
    }
}
