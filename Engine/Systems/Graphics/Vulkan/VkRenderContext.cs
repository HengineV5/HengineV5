using RenderLib;
using RenderLib.Vulkan;
using UtilLib.Memory;

namespace Engine
{
	public class VkRenderContext
	{
		public FixedBuffer16<GpuSampler> samplers;

		public RenderPipeline<VulkanStorage, HengineRenderGraph<VulkanStorage>, PipelineContainerLayer, RenderPassId> pipeline;

		VulkanStorageOwner storageOwner;

		public VkRenderContext(VkContext context)
		{
			this.storageOwner = new VulkanStorageOwner(context);
		}

		public VulkanStorage Storage => storageOwner.Storage;

		public void Setup()
		{
			VulkanStorage storage = storageOwner.Storage;
			VulkanStorage.Initialize(ref storage);

			samplers = new FixedBuffer16<GpuSampler>();
			for (int i = 0; i < 16; i++)
			{
				samplers[i] = VulkanStorage.CreateSampler(ref storage, 0);
			}

			samplers[8] = VulkanStorage.CreateSampler(ref storage, 5);

			pipeline = RenderPipeline<VulkanStorage, HengineRenderGraph<VulkanStorage>, PipelineContainerLayer, RenderPassId>.Create(ref storage);
		}
	}
}
