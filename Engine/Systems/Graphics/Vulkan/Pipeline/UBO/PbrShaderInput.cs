using EnCS;
using Engine.Graphics;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Engine
{
	public struct PbrShaderInput : IUniformBufferObject<PbrShaderInput>
	{
		public MappedMemory<PbrUniformBufferObject> ubo;
		public MappedMemory<PbrMaterialInfo> material;
		public FixedArray4<MappedMemory<Light>> lights;

		public unsafe static DescriptorSet Create(VkContext context, DescriptorPool descriptorPool)
		{
			DescriptorSetLayout layout = GetLayout(context);

			DescriptorSetAllocateInfo allocInfo = new();
			allocInfo.SType = StructureType.DescriptorSetAllocateInfo;
			allocInfo.DescriptorPool = descriptorPool;
			allocInfo.DescriptorSetCount = 1;
			allocInfo.PSetLayouts = &layout;

			var result = context.vk.AllocateDescriptorSets(context.device, allocInfo, out DescriptorSet descriptorSet);
			if (result != Result.Success)
				throw new Exception("Failed to allocate vkDescriptorSets");

            return descriptorSet;
		}

		public unsafe static PbrShaderInput Map(VkContext context, DescriptorSet descriptorSet)
		{
			Buffer uniformBuffer = VulkanHelper.CreateBuffer(context, BufferUsageFlags.UniformBufferBit, 704);
			DeviceMemory uniformBuffersMemory = VulkanHelper.CreateBufferMemory(context, uniformBuffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

			void* dataPtr;
			context.vk.MapMemory(context.device, uniformBuffersMemory, 0, 704, 0, &dataPtr);

            PbrShaderInput shaderInput = new PbrShaderInput();

			// TODO: Convert this builder to source generator
			var uniformBufferBuilder = new UniformBufferBuilder(descriptorSet, uniformBuffer)
						.Variable<PbrUniformBufferObject>(0)
						.Variable<PbrMaterialInfo>(10)
						.Array<Light>(11, 4);

			shaderInput.ubo = uniformBufferBuilder.GetElement<PbrUniformBufferObject>(dataPtr, 0);
			shaderInput.material = uniformBufferBuilder.GetElement<PbrMaterialInfo>(dataPtr, 1);
			for (int b = 0; b < 4; b++)
			{
				shaderInput.lights[b] = uniformBufferBuilder.GetElement<Light>(dataPtr, 2 + (uint)b);
			}

			uniformBufferBuilder.UpdateDescriptorSet(context);

            return shaderInput;
		}

		public static DescriptorSetLayout GetLayout(VkContext context)
		{
			return new DescriptorSetLayoutBuilder()
				.Uniform(ShaderStageFlags.VertexBit, 1)			// UBO
				.Samplers(ShaderStageFlags.FragmentBit, 1, 9)	// PBR textures
				.Uniform(ShaderStageFlags.FragmentBit, 1)		// Lights
				.Uniform(ShaderStageFlags.FragmentBit, 4)		// Cubemap
				.Build(context);
		}
	}

	public struct GuiShaderInput : IUniformBufferObject<GuiShaderInput>
	{
		public MappedMemory<GuiUniformBufferObject> ubo;

		public unsafe static DescriptorSet Create(VkContext context, DescriptorPool descriptorPool)
		{
			DescriptorSetLayout layout = GetLayout(context);

			DescriptorSetAllocateInfo allocInfo = new();
			allocInfo.SType = StructureType.DescriptorSetAllocateInfo;
			allocInfo.DescriptorPool = descriptorPool;
			allocInfo.DescriptorSetCount = 1;
			allocInfo.PSetLayouts = &layout;

			var result = context.vk.AllocateDescriptorSets(context.device, allocInfo, out DescriptorSet descriptorSet);
			if (result != Result.Success)
				throw new Exception("Failed to allocate vkDescriptorSets");

			return descriptorSet;
		}

		public unsafe static GuiShaderInput Map(VkContext context, DescriptorSet descriptorSet)
		{
			Buffer uniformBuffer = VulkanHelper.CreateBuffer(context, BufferUsageFlags.UniformBufferBit, 704);
			DeviceMemory uniformBuffersMemory = VulkanHelper.CreateBufferMemory(context, uniformBuffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

			void* dataPtr;
			context.vk.MapMemory(context.device, uniformBuffersMemory, 0, 704, 0, &dataPtr);

			var shaderInput = new GuiShaderInput();

			// TODO: Convert this builder to source generator
			var uniformBufferBuilder = new UniformBufferBuilder(descriptorSet, uniformBuffer)
						.Variable<PbrUniformBufferObject>(0);

			shaderInput.ubo = uniformBufferBuilder.GetElement<GuiUniformBufferObject>(dataPtr, 0);

            uniformBufferBuilder.UpdateDescriptorSet(context);
			return shaderInput;
		}

		public static DescriptorSetLayout GetLayout(VkContext context)
		{
			return new DescriptorSetLayoutBuilder()
				.Uniform(ShaderStageFlags.VertexBit, 1)         // UBO
				.Build(context);
		}
	}
}
