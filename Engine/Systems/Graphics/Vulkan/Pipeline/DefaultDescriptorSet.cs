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
                        .Variable<PbrMaterial>(9)
                        .Array<Light>(10, 4);

            descriptorSet.shaderInput.ubo = uniformBufferBuilder.GetElement<UniformBufferObject>(dataPtr, 0);
            //framesInFlight.Span[i].uboMemories[a].material = uniformBufferBuilder.GetElement<Material>(dataPtr, 1);
            descriptorSet.shaderInput.material = uniformBufferBuilder.GetElement<PbrMaterial>(dataPtr, 1);
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

        static unsafe DescriptorSetLayout CreateDescriptorSetLayout(VkContext context)
        {
            DescriptorSetLayoutBinding uniformBinding = new();
            uniformBinding.Binding = 0;
            uniformBinding.DescriptorType = DescriptorType.UniformBuffer;
            uniformBinding.DescriptorCount = 1;
            uniformBinding.StageFlags = ShaderStageFlags.VertexBit;
            uniformBinding.PImmutableSamplers = null;

            DescriptorSetLayoutBinding textureSamplerBinding = new();
            textureSamplerBinding.Binding = 1;
            textureSamplerBinding.DescriptorType = DescriptorType.CombinedImageSampler;
            textureSamplerBinding.DescriptorCount = 1;
            textureSamplerBinding.StageFlags = ShaderStageFlags.FragmentBit;
            textureSamplerBinding.PImmutableSamplers = null;

            DescriptorSetLayoutBinding albedoSamplerBinding = new();
            albedoSamplerBinding.Binding = 2;
            albedoSamplerBinding.DescriptorType = DescriptorType.CombinedImageSampler;
            albedoSamplerBinding.DescriptorCount = 1;
            albedoSamplerBinding.StageFlags = ShaderStageFlags.FragmentBit;
            albedoSamplerBinding.PImmutableSamplers = null;

            DescriptorSetLayoutBinding normalSamplerBinding = new();
            normalSamplerBinding.Binding = 3;
            normalSamplerBinding.DescriptorType = DescriptorType.CombinedImageSampler;
            normalSamplerBinding.DescriptorCount = 1;
            normalSamplerBinding.StageFlags = ShaderStageFlags.FragmentBit;
            normalSamplerBinding.PImmutableSamplers = null;

            DescriptorSetLayoutBinding metallicSamplerBinding = new();
            metallicSamplerBinding.Binding = 4;
            metallicSamplerBinding.DescriptorType = DescriptorType.CombinedImageSampler;
            metallicSamplerBinding.DescriptorCount = 1;
            metallicSamplerBinding.StageFlags = ShaderStageFlags.FragmentBit;
            metallicSamplerBinding.PImmutableSamplers = null;

            DescriptorSetLayoutBinding roughnessSamplerBinding = new();
            roughnessSamplerBinding.Binding = 5;
            roughnessSamplerBinding.DescriptorType = DescriptorType.CombinedImageSampler;
            roughnessSamplerBinding.DescriptorCount = 1;
            roughnessSamplerBinding.StageFlags = ShaderStageFlags.FragmentBit;
            roughnessSamplerBinding.PImmutableSamplers = null;

            /*
			DescriptorSetLayoutBinding aoSamplerBinding = new();
			aoSamplerBinding.Binding = 6;
			aoSamplerBinding.DescriptorType = DescriptorType.CombinedImageSampler;
			aoSamplerBinding.DescriptorCount = 1;
			aoSamplerBinding.StageFlags = ShaderStageFlags.FragmentBit;
			aoSamplerBinding.PImmutableSamplers = null;
			*/

            DescriptorSetLayoutBinding skyboxSamplerBinding = new();
            skyboxSamplerBinding.Binding = 6;
            skyboxSamplerBinding.DescriptorType = DescriptorType.CombinedImageSampler;
            skyboxSamplerBinding.DescriptorCount = 1;
            skyboxSamplerBinding.StageFlags = ShaderStageFlags.FragmentBit;
            skyboxSamplerBinding.PImmutableSamplers = null;

            DescriptorSetLayoutBinding skyboxIrradianceSamplerBinding = new();
            skyboxIrradianceSamplerBinding.Binding = 7;
            skyboxIrradianceSamplerBinding.DescriptorType = DescriptorType.CombinedImageSampler;
            skyboxIrradianceSamplerBinding.DescriptorCount = 1;
            skyboxIrradianceSamplerBinding.StageFlags = ShaderStageFlags.FragmentBit;
            skyboxIrradianceSamplerBinding.PImmutableSamplers = null;

            DescriptorSetLayoutBinding skyboxSpecularSamplerBinding = new();
            skyboxSpecularSamplerBinding.Binding = 8;
            skyboxSpecularSamplerBinding.DescriptorType = DescriptorType.CombinedImageSampler;
            skyboxSpecularSamplerBinding.DescriptorCount = 1;
            skyboxSpecularSamplerBinding.StageFlags = ShaderStageFlags.FragmentBit;
            skyboxSpecularSamplerBinding.PImmutableSamplers = null;

            DescriptorSetLayoutBinding materialBinding = new();
            materialBinding.Binding = 9;
            materialBinding.DescriptorType = DescriptorType.UniformBuffer;
            materialBinding.DescriptorCount = 1;
            materialBinding.StageFlags = ShaderStageFlags.FragmentBit;
            materialBinding.PImmutableSamplers = null;

            DescriptorSetLayoutBinding lightBinding = new();
            lightBinding.Binding = 10;
            lightBinding.DescriptorType = DescriptorType.UniformBuffer;
            lightBinding.DescriptorCount = 4;
            lightBinding.StageFlags = ShaderStageFlags.FragmentBit;
            lightBinding.PImmutableSamplers = null;

            DescriptorSetLayoutCreateInfo createInfo = new();
            createInfo.SType = StructureType.DescriptorSetLayoutCreateInfo;
            createInfo.BindingCount = 11;

            DescriptorSetLayoutBinding* bindingsPtr = stackalloc DescriptorSetLayoutBinding[11];
            bindingsPtr[0] = uniformBinding;
            bindingsPtr[1] = textureSamplerBinding;
            bindingsPtr[2] = albedoSamplerBinding;
            bindingsPtr[3] = normalSamplerBinding;
            bindingsPtr[4] = metallicSamplerBinding;
            bindingsPtr[5] = roughnessSamplerBinding;
            bindingsPtr[6] = skyboxSamplerBinding;
            bindingsPtr[7] = skyboxIrradianceSamplerBinding;
            bindingsPtr[8] = skyboxSpecularSamplerBinding;
            bindingsPtr[9] = materialBinding;
            bindingsPtr[10] = lightBinding;

            createInfo.PBindings = bindingsPtr;

            var result = context.vk.CreateDescriptorSetLayout(context.device, createInfo, null, out DescriptorSetLayout descriptorSetLayout);
            if (result != Result.Success)
                throw new Exception("Failed to create vkDescriptorSetLayout");

            return descriptorSetLayout;
        }
    }
}
