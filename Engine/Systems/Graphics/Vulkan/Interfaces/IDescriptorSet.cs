using Silk.NET.Vulkan;

namespace Engine
{
    public interface IDescriptorSet<TSelf> where TSelf : struct, IDescriptorSet<TSelf>
	{
		static abstract TSelf Create(VkContext context, DescriptorPool descriptorPool);

		static abstract DescriptorSet GetDescriptorSet(ref TSelf self);

		static abstract DescriptorSetLayout GetLayout(VkContext context);

		static abstract DescriptorPool GetPool(VkContext context, uint count);
	}
}
