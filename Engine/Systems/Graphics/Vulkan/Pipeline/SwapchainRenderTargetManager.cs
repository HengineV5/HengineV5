using Silk.NET.Vulkan;
using EnCS;
using Silk.NET.Vulkan.Video;
using System.Buffers;
using Silk.NET.SDL;

namespace Engine
{
    public struct SwapchainRenderTargetManager<TDescriptorSet> : IRenderTargetManager<SwapchainRenderTargetManager<TDescriptorSet>, TDescriptorSet, DefaultRenderPassInfo, DefaultPipelineInfo>
		where TDescriptorSet : struct, IDescriptorSet<TDescriptorSet>
	{
        const int MAX_FRAMES_IN_FLIGHT = 3;

		Swapchain swapchain;

		Memory<ImageView> imageViews;
		Memory<Framebuffer> frameBuffers;
		Memory<FrameInFlight<TDescriptorSet>> framesInFlight;

		int currentFrame;

        public SwapchainRenderTargetManager(Swapchain swapchain)
        {
			this.swapchain = swapchain;
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

        public static bool PresentTarget(VkContext context, ref SwapchainRenderTargetManager<TDescriptorSet> self, ref RenderTarget<TDescriptorSet> renderTarget)
        {
            var presentResult = VulkanHelper.QueuePresent(context, self.swapchain.GetPresentQueue(), self.swapchain.GetSwapchain(), renderTarget.imageIndex, renderTarget.frame.imageAvailable);
			if (presentResult == Result.ErrorOutOfDateKhr || presentResult == Result.SuboptimalKhr)
			{
				return false;
				throw new Exception();
				//recreateSwapChain();
                //RecreateSwapchain(context, )
			}

			// TODO: Improve
			self.currentFrame = (self.currentFrame + 1) % MAX_FRAMES_IN_FLIGHT;
			return true;
        }

        public static DefaultPipelineInfo GetPipelineInfo(VkContext context, ref SwapchainRenderTargetManager<TDescriptorSet> self)
        {
			return new DefaultPipelineInfo(self.swapchain.GetExtent());
        }

        public static DefaultRenderPassInfo GetRendePassInfo(VkContext context, ref SwapchainRenderTargetManager<TDescriptorSet> self)
        {
			return new DefaultRenderPassInfo(self.swapchain.GetSurfaceFormat().Format, Swapchain.GetDepthFormat(context));
        }

        public static Rect2D GetRenderArea(ref SwapchainRenderTargetManager<TDescriptorSet> self)
        {
			return new(new(), self.swapchain.GetExtent());
        }

        public static SwapchainRenderTargetManager<TDescriptorSet> Create(VkContext context, CommandPool commandPool)
        {
			SurfaceKHR surface = CreateSurface(context);
			Swapchain swapchain = Swapchain.Create(context, surface, commandPool);

            return new SwapchainRenderTargetManager<TDescriptorSet>(swapchain);
        }

		public static void Init(VkContext context, ref SwapchainRenderTargetManager<TDescriptorSet> self, RenderPass compatibleRenderPass, CommandPool commandPool)
		{
			self.imageViews = new ImageView[self.swapchain.GetImageCount()];
			self.swapchain.GetImages(context, self.imageViews.Span);

			self.frameBuffers = new Framebuffer[self.swapchain.GetImageCount()];
			for (int i = 0; i < self.frameBuffers.Span.Length; i++)
			{
				self.frameBuffers.Span[i] = VulkanHelper.CreateFrameBuffer(context, [self.imageViews.Span[i], self.swapchain.GetDepthImage()], compatibleRenderPass, self.swapchain.GetExtent());
			}

			DescriptorPool descriptorPool = TDescriptorSet.GetPool(context, MAX_FRAMES_IN_FLIGHT * 16);

			self.framesInFlight = new FrameInFlight<TDescriptorSet>[MAX_FRAMES_IN_FLIGHT];
			for (int i = 0; i < self.framesInFlight.Span.Length; i++)
			{
				FixedArray16<TDescriptorSet> descriptorSets = new FixedArray16<TDescriptorSet>();

				for (int a = 0; a < 16; a++)
				{
					descriptorSets[a] = TDescriptorSet.Create(context, descriptorPool);
				}

				self.framesInFlight.Span[i] = new FrameInFlight<TDescriptorSet>(
					VulkanHelper.CreateSemaphore(context),
					VulkanHelper.CreateSemaphore(context),
					VulkanHelper.CreateFence(context, FenceCreateFlags.SignaledBit),
					VulkanHelper.CreateCommandBuffer(context, commandPool),
					descriptorSets
				);
			}
		}

		public static unsafe void Dispose(VkContext context, ref SwapchainRenderTargetManager<TDescriptorSet> self, CommandPool commandPool)
		{
			self.swapchain.Dispose(context);

			foreach (var frameBuffer in self.frameBuffers.Span)
			{
				context.vk.DestroyFramebuffer(context.device, frameBuffer, null);
			}

			foreach (var imageView in self.imageViews.Span)
			{
				context.vk.DestroyImageView(context.device, imageView, null);
			}

			foreach (var frame in self.framesInFlight.Span)
			{
				context.vk.FreeCommandBuffers(context.device, commandPool, [frame.commandBuffer]);
			}
		}

		static unsafe SurfaceKHR CreateSurface(VkContext context)
		{
			return context.window.VkSurface.Create<AllocationCallbacks>(context.instance.ToHandle(), null).ToSurface();
		}

		/*
		static void RecreateSwapchain(VkContext context, SurfaceKHR surface, CommandPool commandPool)
		{
			context.vk.DeviceWaitIdle(context.device);

			swapchain.Dispose(context);
			DisposeSwapchain(context, commandPool);

			swapchain = Swapchain.Create(context, surface, commandPool);
			meshRenderPass = CreateMeshRenderPass(context, swapchain.GetSurfaceFormat().Format, Swapchain.GetDepthFormat(context));
			skyboxRenderPass = CreateSkyboxRenderPass(context, swapchain.GetSurfaceFormat().Format, Swapchain.GetDepthFormat(context));
			pipelineLayout = CreatePipelineLayout(context, descriptorSetLayout);

			var pbrShader = Shader.FromFiles("Shaders/Pbr/PbrVert.spv", "Shaders/Pbr/PbrFrag.spv");
			meshPipeline = CreateGraphicsPipeline(context, swapchain.GetExtent(), pipelineLayout, skyboxRenderPass, pbrShader);

			var shaderSkybox = Shader.FromFiles("Shaders/Pbr/SkyboxVert.spv", "Shaders/Pbr/SkyboxFrag.spv");
			skyboxPipeline = CreateGraphicsPipeline(context, swapchain.GetExtent(), pipelineLayout, skyboxRenderPass, shaderSkybox);

			swapchain.GetImages(context, swapchainImages.Span);
			for (int i = 0; i < frameBuffers.Span.Length; i++)
			{
				frameBuffers.Span[i] = VulkanHelper.CreateFrameBuffer(context, [swapchainImages.Span[i], swapchain.GetDepthImage()], skyboxRenderPass, swapchain.GetExtent());
			}

			for (int i = 0; i < framesInFlight.Span.Length; i++)
			{
				framesInFlight.Span[i].commandBuffer = VulkanHelper.CreateCommandBuffer(context, commandPool);
			}
		}
        */
	}
}
