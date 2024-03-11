using Silk.NET.Vulkan;

namespace Engine
{
    public interface IRenderPassContainer<TSelf, TRenderPassInfo, TEnum>
		where TSelf : IRenderPassContainer<TSelf, TRenderPassInfo, TEnum>
		where TRenderPassInfo : struct
        where TEnum : Enum
    {
        static abstract TSelf Create(VkContext context, in TRenderPassInfo renderPassInfo);

        static abstract void Dispose(VkContext context, ref TSelf self);

		static abstract RenderPass Get(TEnum mask, ref TSelf self);

        static abstract RenderPass GetCompatibleRenderPass(ref TSelf self);
    }
}
