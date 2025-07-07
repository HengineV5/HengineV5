using Silk.NET.Vulkan;
using EnCS;
using Silk.NET.Vulkan.Video;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Hengine
{
	public struct SwapchainRenderTargetManager : IRenderTargetManager<SwapchainRenderTargetManager, DefaultRenderPassInfo, DefaultPipelineInfo>
	{
        const int MAX_FRAMES_IN_FLIGHT = 3;

		Swapchain swapchain;

		Memory<ImageView> imageViews;
		Memory<Framebuffer> frameBuffers;
		Memory<FrameInFlight> framesInFlight;

		int currentFrame;

        public SwapchainRenderTargetManager(Swapchain swapchain)
        {
			this.swapchain = swapchain;
			this.currentFrame = 0;
        }

        public static RenderTarget AquireRenderTarget(VkContext context, ref SwapchainRenderTargetManager self)
        {
            var renderTarget = new RenderTarget()
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

        public static bool PresentTarget(VkContext context, ref SwapchainRenderTargetManager self, ref RenderTarget renderTarget)
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

        public static DefaultPipelineInfo GetPipelineInfo(VkContext context, ref SwapchainRenderTargetManager self)
        {
			return new DefaultPipelineInfo(self.swapchain.GetExtent());
        }

        public static DefaultRenderPassInfo GetRendePassInfo(VkContext context, ref SwapchainRenderTargetManager self)
        {
			return new DefaultRenderPassInfo(self.swapchain.GetSurfaceFormat().Format, Swapchain.GetDepthFormat(context));
        }

        public static Rect2D GetRenderArea(ref SwapchainRenderTargetManager self)
        {
			return new(new(), self.swapchain.GetExtent());
        }

        public static SwapchainRenderTargetManager Create(VkContext context, CommandPool commandPool)
        {
			SurfaceKHR surface = CreateSurface(context);
			Swapchain swapchain = Swapchain.Create(context, surface, commandPool);

            return new SwapchainRenderTargetManager(swapchain);
        }

		public static void Init(VkContext context, ref SwapchainRenderTargetManager self, RenderPass compatibleRenderPass, CommandPool commandPool)
		{
			self.imageViews = new ImageView[self.swapchain.GetImageCount()];
			self.swapchain.GetImages(context, self.imageViews.Span);

			self.frameBuffers = new Framebuffer[self.swapchain.GetImageCount()];
			for (int i = 0; i < self.frameBuffers.Span.Length; i++)
			{
				self.frameBuffers.Span[i] = VulkanHelper.CreateFrameBuffer(context, [self.imageViews.Span[i], self.swapchain.GetDepthImage()], compatibleRenderPass, self.swapchain.GetExtent());
			}

			self.framesInFlight = new FrameInFlight[MAX_FRAMES_IN_FLIGHT];
			for (int i = 0; i < self.framesInFlight.Span.Length; i++)
			{
				self.framesInFlight.Span[i] = new FrameInFlight(
					VulkanHelper.CreateSemaphore(context),
					VulkanHelper.CreateSemaphore(context),
					VulkanHelper.CreateFence(context, FenceCreateFlags.SignaledBit),
					VulkanHelper.CreateCommandBuffer(context, commandPool)
				);
			}
		}

		public static unsafe void Dispose(VkContext context, ref SwapchainRenderTargetManager self, CommandPool commandPool)
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint GetFramesInFlight()
		 => MAX_FRAMES_IN_FLIGHT;

		public static DescriptorSet GetDescriptorSet<TDescriptorContainer, TPipelineEnum>(TPipelineEnum layer, ref TDescriptorContainer descriptorContainer, uint idx, ref SwapchainRenderTargetManager self)
			where TDescriptorContainer : struct, IDescriptorContainer<TDescriptorContainer, TPipelineEnum>
			where TPipelineEnum : Enum
		{
			return TDescriptorContainer.GetDescriptorSet(layer, (uint)self.currentFrame, idx, ref descriptorContainer);
		}

		public static ref TUbo GetUbo<TUbo, TDescriptorContainer, TPipelineEnum>(uint idx, scoped ref SwapchainRenderTargetManager self)
			where TUbo : struct, IUniformBufferObject<TUbo>
			where TDescriptorContainer : struct, IDescriptorContainer<TDescriptorContainer, TPipelineEnum>
			where TPipelineEnum : Enum
		{
			return ref TDescriptorContainer.GetUbo<TUbo>((uint)self.currentFrame, idx);
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
