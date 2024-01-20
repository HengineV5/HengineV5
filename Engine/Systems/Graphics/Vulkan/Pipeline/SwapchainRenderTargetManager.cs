using Silk.NET.Vulkan;
using EnCS;
using Silk.NET.Vulkan.Video;
using System.Buffers;

namespace Engine
{
    public struct SwapchainRenderTargetManager<TDescriptorSet> : IRenderTargetManager<SwapchainRenderTargetManager<TDescriptorSet>, TDescriptorSet, DefaultRenderPassInfo, DefaultPipelineInfo>
		where TDescriptorSet : struct, IDescriptorSet<TDescriptorSet>
	{
        const int MAX_FRAMES_IN_FLIGHT = 3;

		RenderPass compatibleRenderPass;
		Swapchain swapchain;

		Memory<ImageView> imageViews;
		Memory<Framebuffer> frameBuffers;
		Memory<FrameInFlight<TDescriptorSet>> framesInFlight;

		int currentFrame;

        public SwapchainRenderTargetManager(Swapchain swapchain, RenderPass compatibleRenderPass, Memory<ImageView> imageViews, Memory<Framebuffer> frameBuffers, Memory<FrameInFlight<TDescriptorSet>> framesInFlight)
        {
			this.swapchain = swapchain;
			this.compatibleRenderPass = compatibleRenderPass;
			this.imageViews = imageViews;
			this.frameBuffers = frameBuffers;
			this.framesInFlight = framesInFlight;
			this.currentFrame = 0;
        }

        public static RenderTarget<TDescriptorSet> AquireRenderTarget(VkContext context, ref SwapchainRenderTargetManager<TDescriptorSet> self)
        {
            var renderTarget = new RenderTarget<TDescriptorSet>()
			{
				frame = self.framesInFlight.Span[self.currentFrame],
			};

            VulkanHelper.WaitForFence(context, renderTarget.frame.inFlight);

            var aquireResult = self.swapchain.AcquireNextImageIndex(context, renderTarget.frame.imageAvailable, out renderTarget.imageIndex);
			renderTarget.framebuffer = self.frameBuffers.Span[(int)renderTarget.imageIndex];

            //if (aquireResult == Result.ErrorOutOfDateKhr)
            if (aquireResult != Result.Success)
				throw new Exception();
            //return aquireResult;

            return renderTarget;
        }

		// TODO: Add image index as generic parameter in RenderTarget
        public static void PresentTarget(VkContext context, ref SwapchainRenderTargetManager<TDescriptorSet> self, ref RenderTarget<TDescriptorSet> renderTarget)
        {
            VulkanHelper.QueuePresent(context, self.swapchain.GetPresentQueue(), self.swapchain.GetSwapchain(), renderTarget.imageIndex, renderTarget.frame.imageAvailable);
            //Console.WriteLine($"P: {renderTarget.imageIndex}, F: {self.currentFrame}");

            // TODO: Improve
            self.currentFrame = (self.currentFrame + 1) % MAX_FRAMES_IN_FLIGHT;
        }

        public static DefaultPipelineInfo GetPipelineInfo(VkContext context, ref SwapchainRenderTargetManager<TDescriptorSet> self)
        {
			return new DefaultPipelineInfo(self.swapchain.GetExtent(), self.compatibleRenderPass);
        }

        public static DefaultRenderPassInfo GetRendePassInfo(VkContext context, ref SwapchainRenderTargetManager<TDescriptorSet> self)
        {
			return new DefaultRenderPassInfo(self.swapchain.GetSurfaceFormat().Format, Swapchain.GetDepthFormat(context));
        }

        public static Rect2D GetRenderArea(ref SwapchainRenderTargetManager<TDescriptorSet> self)
        {
			return new(new(), self.swapchain.GetExtent());
        }

        public static SwapchainRenderTargetManager<TDescriptorSet> Create(VkContext context, Swapchain swapchain, RenderPass compatibleRenderPass, CommandPool commandPool)
        {
            Memory<ImageView> swapchainImages = new ImageView[swapchain.GetImageCount()];
            swapchain.GetImages(context, swapchainImages.Span);

            Memory<Framebuffer> frameBuffers = new Framebuffer[swapchain.GetImageCount()];
            for (int i = 0; i < frameBuffers.Span.Length; i++)
            {
                frameBuffers.Span[i] = VulkanHelper.CreateFrameBuffer(context, [swapchainImages.Span[i], swapchain.GetDepthImage()], compatibleRenderPass, swapchain.GetExtent());
            }

            DescriptorPool descriptorPool = TDescriptorSet.GetPool(context, MAX_FRAMES_IN_FLIGHT * 16);

            Memory<FrameInFlight<TDescriptorSet>> framesInFlight = new FrameInFlight<TDescriptorSet>[MAX_FRAMES_IN_FLIGHT];
            for (int i = 0; i < framesInFlight.Span.Length; i++)
            {
                FixedArray16<TDescriptorSet> descriptorSets = new FixedArray16<TDescriptorSet>();

                for (int a = 0; a < 16; a++)
                {
                    descriptorSets[a] = TDescriptorSet.Create(context, descriptorPool);
                }

                framesInFlight.Span[i] = new FrameInFlight<TDescriptorSet>(
                    VulkanHelper.CreateSemaphore(context),
                    VulkanHelper.CreateSemaphore(context),
                    VulkanHelper.CreateFence(context, FenceCreateFlags.SignaledBit),
                    VulkanHelper.CreateCommandBuffer(context, commandPool),
                    descriptorSets
                );
            }

            return new SwapchainRenderTargetManager<TDescriptorSet>(swapchain, compatibleRenderPass, swapchainImages, frameBuffers, framesInFlight);
        }
    }
}
