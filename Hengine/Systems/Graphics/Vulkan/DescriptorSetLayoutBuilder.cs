using Silk.NET.Vulkan;
using System.Buffers;

namespace Hengine
{
	public ref struct DescriptorSetLayoutBuilder
	{
		int idx = 0;
		Memory<DescriptorSetLayoutBinding> layoutBindings;

        public DescriptorSetLayoutBuilder()
        {
			layoutBindings = ArrayPool<DescriptorSetLayoutBinding>.Shared.Rent(32);
        }

        public unsafe DescriptorSetLayoutBuilder Uniform(ShaderStageFlags stageFlags, uint descriptorCount)
		{
			DescriptorSetLayoutBinding layoutBinding = new();
			layoutBinding.Binding = (uint)idx;
			layoutBinding.DescriptorType = DescriptorType.UniformBuffer;
			layoutBinding.DescriptorCount = descriptorCount;
			layoutBinding.StageFlags = stageFlags;
			layoutBinding.PImmutableSamplers = null;

			layoutBindings.Span[idx++] = layoutBinding;
			return this;
		}

		public DescriptorSetLayoutBuilder Uniforms(ShaderStageFlags stageFlags, uint descriptorCount, uint uniformCount)
		{
			for (int i = 0; i < uniformCount; i++)
				Uniform(stageFlags, descriptorCount);

			return this;
		}

		public unsafe DescriptorSetLayoutBuilder Sampler(ShaderStageFlags stageFlags, uint descriptorCount)
		{
			DescriptorSetLayoutBinding layoutBinding = new();
			layoutBinding.Binding = (uint)idx;
			layoutBinding.DescriptorType = DescriptorType.CombinedImageSampler;
			layoutBinding.DescriptorCount = descriptorCount;
			layoutBinding.StageFlags = stageFlags;
			layoutBinding.PImmutableSamplers = null;

			layoutBindings.Span[idx++] = layoutBinding;
			return this;
		}

		public DescriptorSetLayoutBuilder Samplers(ShaderStageFlags stageFlags, uint descriptorCount, uint samplerCount)
		{
			for (int i = 0; i < samplerCount; i++)
				Sampler(stageFlags, descriptorCount);

			return this;
		}

		public unsafe DescriptorSetLayout Build(VkContext context)
		{
			DescriptorSetLayoutCreateInfo createInfo = new();
			createInfo.SType = StructureType.DescriptorSetLayoutCreateInfo;
			createInfo.BindingCount = (uint)idx;

			fixed (DescriptorSetLayoutBinding* bindingsPtr = layoutBindings.Span)
			{
				createInfo.PBindings = bindingsPtr;
			}

			var result = context.vk.CreateDescriptorSetLayout(context.device, createInfo, null, out DescriptorSetLayout descriptorSetLayout);
			if (result != Result.Success)
				throw new Exception("Failed to create vkDescriptorSetLayout");

			return descriptorSetLayout;
		}
	}
}
