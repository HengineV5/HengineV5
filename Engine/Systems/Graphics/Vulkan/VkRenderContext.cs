using RenderLib;
using RenderLib.Vulkan;
using Silk.NET.Vulkan;
using UtilLib.Memory;

using Backend = RenderLib.Backend;

namespace Engine
{
	public class VkRenderContext
	{
		VkContext context;

		public CommandPool commandPool;
		public FixedBuffer16<Sampler> samplers;

		public RenderPipeline<Backend.Vulkan, HengineRenderGraph, PipelineContainerLayer, RenderPassId> pipeline;

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

			Backend.Vulkan backend = Backend.Vulkan.Create(context);
			commandPool = backend.CommandPool;

			pipeline = RenderPipeline<Backend.Vulkan, HengineRenderGraph, PipelineContainerLayer, RenderPassId>.Create(ref backend);
		}
	}
}
