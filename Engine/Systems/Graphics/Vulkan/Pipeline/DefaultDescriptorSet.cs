using EnCS;
using Engine.Graphics;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Engine
{
	public struct PbrShaderInput : IUniformBufferObject<PbrShaderInput>
	{
		public MappedMemory<UniformBufferObject> ubo;
		public MappedMemory<PbrMaterialInfo> material;
		public FixedArray4<MappedMemory<Light>> lights;

		public unsafe static DescriptorSet Create(VkContext context, DescriptorPool descriptorPool)
		{
			DescriptorSetLayout layout = CreateDescriptorSetLayout(context);

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

			// TODO: Reorder
			var uniformBufferBuilder = new UniformBufferBuilder(descriptorSet, uniformBuffer)
						.Variable<UniformBufferObject>(0)
						.Variable<PbrMaterialInfo>(10)
						.Array<Light>(11, 4);

			shaderInput.ubo = uniformBufferBuilder.GetElement<UniformBufferObject>(dataPtr, 0);
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
			return CreateDescriptorSetLayout(context);
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
	}

	static class DescriptorSetGroupCache<TDescriptorSet> where TDescriptorSet : struct, IUniformBufferObject<TDescriptorSet>
	{
		public static bool Initialized = false;
		public static uint size;
		public static Memory<TDescriptorSet> mapped;

		public static ref TDescriptorSet GetMapped(uint frame, uint idx)
		{
			return ref mapped.Span[(int)(frame * size + idx)];
		}
	}


	public struct DescriptorSetGroup<TDescriptorSet> where TDescriptorSet : struct, IUniformBufferObject<TDescriptorSet>
    {
        public Memory<DescriptorSet> descriptorSets;
		public uint size;

        public DescriptorSetGroup(Memory<DescriptorSet> descriptorSets, uint size)
        {
            this.descriptorSets = descriptorSets;
			this.size = size;
        }

		public DescriptorSet GetDescriptorSet(uint frame, uint idx)
		{
			return descriptorSets.Span[(int)(frame * size + idx)];
		}

        public static DescriptorSetGroup<TDescriptorSet> Create(VkContext context, DescriptorPool pool, uint frames, uint size)
        {
            DescriptorSetGroup<TDescriptorSet> group = new DescriptorSetGroup<TDescriptorSet>(new DescriptorSet[frames * size], size);

			if (DescriptorSetGroupCache<TDescriptorSet>.Initialized)
				throw new Exception();

			DescriptorSetGroupCache<TDescriptorSet>.mapped = new TDescriptorSet[frames * size];
			DescriptorSetGroupCache<TDescriptorSet>.size = size;

			for (int i = 0; i < group.descriptorSets.Length; i++)
			{
				group.descriptorSets.Span[i] = TDescriptorSet.Create(context, pool);
				DescriptorSetGroupCache<TDescriptorSet>.mapped.Span[i] = TDescriptorSet.Map(context, group.descriptorSets.Span[i]);
			}

			DescriptorSetGroupCache<TDescriptorSet>.Initialized = true;

            return group;
		}
	}

	public struct DescriptorSetContainer : IDescriptorContainer<DescriptorSetContainer, PipelineContainerLayer>
	{
        DescriptorSetGroup<PbrShaderInput> pbrDescriptors;

		public DescriptorSetContainer(DescriptorSetGroup<PbrShaderInput> pbrDescriptors)
		{
			this.pbrDescriptors = pbrDescriptors;
		}

		public static DescriptorSetContainer Create<TRenderTargetManager, TRenderPassInfo, TPipelineInfo>(VkContext context)
			where TRenderTargetManager : struct, IRenderTargetManager<TRenderTargetManager, TRenderPassInfo, TPipelineInfo>
			where TRenderPassInfo : struct
			where TPipelineInfo : struct
		{
			uint frames = TRenderTargetManager.GetFramesInFlight();
			uint descriptorsPerFrame = 16;

			var pbrPool = VulkanHelper.CreateDescriptorPool(context, frames * descriptorsPerFrame);
			var pbr = DescriptorSetGroup<PbrShaderInput>.Create(context, pbrPool, frames, descriptorsPerFrame);

			return new DescriptorSetContainer(pbr);
		}

		public static DescriptorSet GetDescriptorSet(PipelineContainerLayer layer, uint frame, uint idx, ref DescriptorSetContainer self)
		{
			switch (layer)
			{
				case PipelineContainerLayer.Skybox:
				case PipelineContainerLayer.Pbr:
				case PipelineContainerLayer.Wireframe:
					return self.pbrDescriptors.GetDescriptorSet(frame, idx);
				case PipelineContainerLayer.Gui:
					throw new Exception();
				default:
					throw new Exception();
			}
		}

		public static DescriptorSetLayout GetDescriptorSetLayout(VkContext context, PipelineContainerLayer layer)
		{
			switch (layer)
			{
				case PipelineContainerLayer.Skybox:
				case PipelineContainerLayer.Pbr:
				case PipelineContainerLayer.Wireframe:
					return PbrShaderInput.GetLayout(context);
				case PipelineContainerLayer.Gui:
					throw new Exception();
				default:
					throw new Exception();
			}
		}

		public static ref TUbo GetUbo<TUbo>(uint frame, uint idx) where TUbo : struct, IUniformBufferObject<TUbo>
		{
			return ref DescriptorSetGroupCache<TUbo>.GetMapped(frame, idx);
		}
	}

	public struct DefaultDescriptorSet : IDescriptorSet<DefaultDescriptorSet>
    {
		//public PbrShaderInput shaderInput;
		public DescriptorSet descriptorSet;

        public DefaultDescriptorSet(DescriptorSet descriptorSet)
        {
            this.descriptorSet = descriptorSet;
        }

        public static unsafe DefaultDescriptorSet Create(VkContext context, DescriptorPool descriptorPool)
        {
			DescriptorSetLayout layout = GetLayout(context);
            var defaultDescriptorSet = new DefaultDescriptorSet(new DescriptorSet());

            DescriptorSetAllocateInfo allocInfo = new();
            allocInfo.SType = StructureType.DescriptorSetAllocateInfo;
            allocInfo.DescriptorPool = descriptorPool;
            allocInfo.DescriptorSetCount = 1;
            allocInfo.PSetLayouts = &layout;

            var result = context.vk.AllocateDescriptorSets(context.device, allocInfo, out defaultDescriptorSet.descriptorSet);
            if (result != Result.Success)
                throw new Exception("Failed to allocate vkDescriptorSets");

            /*
            Buffer uniformBuffer = VulkanHelper.CreateBuffer(context, BufferUsageFlags.UniformBufferBit, 704);
            DeviceMemory uniformBuffersMemory = VulkanHelper.CreateBufferMemory(context, uniformBuffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

            void* dataPtr;
            context.vk.MapMemory(context.device, uniformBuffersMemory, 0, 704, 0, &dataPtr);

			// TODO: Reorder
            var uniformBufferBuilder = new UniformBufferBuilder(descriptorSet.descriptorSet, uniformBuffer)
                        .Variable<UniformBufferObject>(0)
                        .Variable<PbrMaterialInfo>(10)
                        .Array<Light>(11, 4);

            descriptorSet.shaderInput.ubo = uniformBufferBuilder.GetElement<UniformBufferObject>(dataPtr, 0);
            descriptorSet.shaderInput.material = uniformBufferBuilder.GetElement<PbrMaterialInfo>(dataPtr, 1);
            for (int b = 0; b < 4; b++)
            {
                descriptorSet.shaderInput.lights[b] = uniformBufferBuilder.GetElement<Light>(dataPtr, 2 + (uint)b);
            }

            uniformBufferBuilder.UpdateDescriptorSet(context);
            */
			return defaultDescriptorSet;
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
