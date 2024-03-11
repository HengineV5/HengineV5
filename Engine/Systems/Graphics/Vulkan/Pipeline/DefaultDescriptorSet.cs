using Engine.Graphics;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Engine
{
    public struct DefaultDescriptorSet : IDescriptorSet<DefaultDescriptorSet>
    {
		public VulkanShaderInput shaderInput;
		public DescriptorSet descriptorSet;

        public DefaultDescriptorSet(DescriptorSet descriptorSet, VulkanShaderInput shaderInput)
        {
            this.descriptorSet = descriptorSet;
            this.shaderInput = shaderInput;
        }

        public static unsafe DefaultDescriptorSet Create(VkContext context, DescriptorPool descriptorPool)
        {
			DescriptorSetLayout layout = GetLayout(context);
            var descriptorSet = new DefaultDescriptorSet(new DescriptorSet(), new VulkanShaderInput());

            DescriptorSetAllocateInfo allocInfo = new();
            allocInfo.SType = StructureType.DescriptorSetAllocateInfo;
            allocInfo.DescriptorPool = descriptorPool;
            allocInfo.DescriptorSetCount = 1;
            allocInfo.PSetLayouts = &layout;

            var result = context.vk.AllocateDescriptorSets(context.device, allocInfo, out descriptorSet.descriptorSet);
            if (result != Result.Success)
                throw new Exception("Failed to allocate vkDescriptorSets");

            Buffer uniformBuffer = VulkanHelper.CreateBuffer(context, BufferUsageFlags.UniformBufferBit, 704);
            DeviceMemory uniformBuffersMemory = VulkanHelper.CreateBufferMemory(context, uniformBuffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

            void* dataPtr;
            context.vk.MapMemory(context.device, uniformBuffersMemory, 0, 704, 0, &dataPtr);

			// TODO: Reorder
            var uniformBufferBuilder = new UniformBufferBuilder(descriptorSet.descriptorSet, uniformBuffer)
                        .Variable<UniformBufferObject>(0)
                        //.Variable<Material>(2)
                        .Variable<PbrMaterialInfo>(10)
                        .Array<Light>(11, 4);

            descriptorSet.shaderInput.ubo = uniformBufferBuilder.GetElement<UniformBufferObject>(dataPtr, 0);
            //framesInFlight.Span[i].uboMemories[a].material = uniformBufferBuilder.GetElement<Material>(dataPtr, 1);
            descriptorSet.shaderInput.material = uniformBufferBuilder.GetElement<PbrMaterialInfo>(dataPtr, 1);
            for (int b = 0; b < 4; b++)
            {
                descriptorSet.shaderInput.lights[b] = uniformBufferBuilder.GetElement<Light>(dataPtr, 2 + (uint)b);
            }

            uniformBufferBuilder.UpdateDescriptorSet(context);
			return descriptorSet;
        }

        public static DescriptorSet GetDescriptorSet(ref DefaultDescriptorSet self)
        {
			return self.descriptorSet;
        }

        public static DescriptorSetLayout GetLayout(VkContext context)
        {
            return CreateDescriptorSetLayout(context);
        }

        public static DescriptorPool GetPool(VkContext context, uint count)
        {
            return VulkanHelper.CreateDescriptorPool(context, count);
        }

        static unsafe DescriptorSetLayoutBinding CreateUniformBinding(uint binding, ShaderStageFlags stageFlags, uint descriptorCount)
        {
            DescriptorSetLayoutBinding layoutBinding = new();
            layoutBinding.Binding = binding;
            layoutBinding.DescriptorType = DescriptorType.UniformBuffer;
            layoutBinding.DescriptorCount = descriptorCount;
            layoutBinding.StageFlags = stageFlags;
            layoutBinding.PImmutableSamplers = null;

            return layoutBinding;
        }

        static unsafe DescriptorSetLayoutBinding CreateSamplerBinding(uint binding, ShaderStageFlags stageFlags, uint descriptorCount)
        {
            DescriptorSetLayoutBinding layoutBinding = new();
            layoutBinding.Binding = binding;
            layoutBinding.DescriptorType = DescriptorType.CombinedImageSampler;
            layoutBinding.DescriptorCount = descriptorCount;
            layoutBinding.StageFlags = stageFlags;
            layoutBinding.PImmutableSamplers = null;

            return layoutBinding;
        }

        static unsafe DescriptorSetLayout CreateDescriptorSetLayout(VkContext context)
        {
            DescriptorSetLayoutCreateInfo createInfo = new();
            createInfo.SType = StructureType.DescriptorSetLayoutCreateInfo;
            createInfo.BindingCount = 12;

            DescriptorSetLayoutBinding* bindingsPtr = stackalloc DescriptorSetLayoutBinding[12];
            bindingsPtr[0] = CreateUniformBinding(0, ShaderStageFlags.VertexBit, 1);
            bindingsPtr[1] = CreateSamplerBinding(1, ShaderStageFlags.FragmentBit, 1);
            bindingsPtr[2] = CreateSamplerBinding(2, ShaderStageFlags.FragmentBit, 1);
            bindingsPtr[3] = CreateSamplerBinding(3, ShaderStageFlags.FragmentBit, 1);
            bindingsPtr[4] = CreateSamplerBinding(4, ShaderStageFlags.FragmentBit, 1);
            bindingsPtr[5] = CreateSamplerBinding(5, ShaderStageFlags.FragmentBit, 1);
            bindingsPtr[6] = CreateSamplerBinding(6, ShaderStageFlags.FragmentBit, 1);
            bindingsPtr[7] = CreateSamplerBinding(7, ShaderStageFlags.FragmentBit, 1);
            bindingsPtr[8] = CreateSamplerBinding(8, ShaderStageFlags.FragmentBit, 1);
            bindingsPtr[9] = CreateSamplerBinding(9, ShaderStageFlags.FragmentBit, 1);
            bindingsPtr[10] = CreateUniformBinding(10, ShaderStageFlags.FragmentBit, 1);
            bindingsPtr[11] = CreateUniformBinding(11, ShaderStageFlags.FragmentBit, 4);

            createInfo.PBindings = bindingsPtr;

            var result = context.vk.CreateDescriptorSetLayout(context.device, createInfo, null, out DescriptorSetLayout descriptorSetLayout);
            if (result != Result.Success)
                throw new Exception("Failed to create vkDescriptorSetLayout");

            return descriptorSetLayout;
        }
    }
}
