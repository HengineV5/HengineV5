using Silk.NET.Vulkan;

namespace Engine
{
	// TODO: Improve
	internal static class DescriptorSetGroupCache<TDescriptorSet> where TDescriptorSet : struct, IUniformBufferObject<TDescriptorSet>
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
        DescriptorSetGroup<GuiShaderInput> guiDescriptors;

		public DescriptorSetContainer(DescriptorSetGroup<PbrShaderInput> pbrDescriptors, DescriptorSetGroup<GuiShaderInput> guiDescriptors)
		{
			this.pbrDescriptors = pbrDescriptors;
			this.guiDescriptors = guiDescriptors;
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

			var guiPool = VulkanHelper.CreateDescriptorPool(context, frames * descriptorsPerFrame);
			var gui = DescriptorSetGroup<GuiShaderInput>.Create(context, guiPool, frames, descriptorsPerFrame);

			return new DescriptorSetContainer(pbr, gui);
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
					return self.guiDescriptors.GetDescriptorSet(frame, idx);
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
					return GuiShaderInput.GetLayout(context);
				default:
					throw new Exception();
			}
		}

		public static ref TUbo GetUbo<TUbo>(uint frame, uint idx) where TUbo : struct, IUniformBufferObject<TUbo>
		{
            return ref DescriptorSetGroupCache<TUbo>.GetMapped(frame, idx);
		}
	}
}
