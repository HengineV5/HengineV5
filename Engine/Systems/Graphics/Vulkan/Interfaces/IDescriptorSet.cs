using Silk.NET.Vulkan;

namespace Engine
{
	public interface IUniformBufferObject<TSelf> where TSelf : struct, IUniformBufferObject<TSelf>
	{
		static abstract DescriptorSet Create(VkContext context, DescriptorPool descriptorPool);

		static abstract TSelf Map(VkContext context, DescriptorSet descriptorSet);
	}

	public interface IDescriptorContainer<TSelf, TEnum>
		where TSelf : struct, IDescriptorContainer<TSelf, TEnum>
		where TEnum : Enum
	{
		static abstract TSelf Create<TRenderTargetManager, TRenderPassInfo, TPipelineInfo>(VkContext context)
			where TRenderTargetManager : struct, IRenderTargetManager<TRenderTargetManager, TRenderPassInfo, TPipelineInfo>
			where TRenderPassInfo : struct
			where TPipelineInfo : struct;

		static abstract DescriptorSet GetDescriptorSet(TEnum layer, uint frame, uint idx, ref TSelf self);

		static abstract DescriptorSetLayout GetDescriptorSetLayout(VkContext context, TEnum layer);

		static abstract ref TUbo GetUbo<TUbo>(uint frame, uint idx) where TUbo : struct, IUniformBufferObject<TUbo>;
	}



    public interface IDescriptorSet<TSelf> where TSelf : struct, IDescriptorSet<TSelf>
	{
		static abstract TSelf Create(VkContext context, DescriptorPool descriptorPool);

		static abstract DescriptorSet GetDescriptorSet(ref TSelf self);

		static abstract DescriptorSetLayout GetLayout(VkContext context);

		static abstract DescriptorPool GetPool(VkContext context, uint count);
	}
}
