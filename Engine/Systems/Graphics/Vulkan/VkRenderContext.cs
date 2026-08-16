using RenderLib;
using RenderLib.Vulkan;
using Silk.NET.Vulkan;
using UtilLib.Memory;

namespace Engine
{
	public class VkRenderContext
	{
		VkContext context;

		public CommandPool commandPool;
		public FixedBuffer16<Sampler> samplers;

		public RenderPipeline<VkContext, SwapchainRenderTargetManager, DefaultRenderPassInfo, DefaultPipelineInfo, DescriptorSetContainer, PipelineContainer, PipelineContainerLayer, RenderPassContainer, RenderPassId> pipeline;

		public VkRenderContext(VkContext context)
		{
			this.context = context;
		}

		public void Setup()
		{
			samplers = new FixedBuffer16<Sampler>();
			for (int i = 0; i < 16; i++)
			{
				samplers[i] = VulkanHelper.CreateSampler(context, 0);
			}

			samplers[8] = VulkanHelper.CreateSampler(context, 5);

			uint graphicsQueueFamily = VulkanHelper.GetGraphicsQueueFamily(context);
			Queue graphicsQueue = VulkanHelper.GetQueue(context, graphicsQueueFamily);

			commandPool = VulkanHelper.CreateCommandPool(context, graphicsQueueFamily);

			pipeline = RenderPipeline<VkContext, SwapchainRenderTargetManager, DefaultRenderPassInfo, DefaultPipelineInfo, DescriptorSetContainer, PipelineContainer, PipelineContainerLayer, RenderPassContainer, RenderPassId>.Create(context, graphicsQueue.ToGpu(), commandPool.ToGpu());
		}
	}
}
