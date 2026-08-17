using RenderLib;
using RenderLib.Vulkan;
using Silk.NET.Vulkan;
using UtilLib.Memory;

using Backend = RenderLib.Backend;

namespace Engine
{
	public class VkRenderContext
	{
		public FixedBuffer16<GpuSampler> samplers;

		public RenderPipeline<Backend.Vulkan, HengineRenderGraph<Backend.Vulkan>, PipelineContainerLayer, RenderPassId> pipeline;

		VulkanStorageOwner storageOwner;

		public VkRenderContext(VkContext context)
		{
			this.storageOwner = new VulkanStorageOwner(context);
		}

		public Backend.Vulkan CreateBackend()
		{
			return storageOwner.CreateBackend();
		}

		public void Setup()
		{
			Backend.Vulkan backend = storageOwner.Initialize();
			samplers = new FixedBuffer16<GpuSampler>();
			for (int i = 0; i < 16; i++)
			{
				samplers[i] = Backend.Vulkan.CreateSampler(ref backend, 0);
			}

			samplers[8] = Backend.Vulkan.CreateSampler(ref backend, 5);

			pipeline = RenderPipeline<Backend.Vulkan, HengineRenderGraph<Backend.Vulkan>, PipelineContainerLayer, RenderPassId>.Create(ref backend);
		}

	}
}
