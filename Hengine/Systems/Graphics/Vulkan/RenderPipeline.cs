using Hengine.Graphics;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System.Runtime.InteropServices;
using Image = Silk.NET.Vulkan.Image;
using Semaphore = Silk.NET.Vulkan.Semaphore;
using System.Runtime.CompilerServices;
using Hengine.Components.Graphics;
using EnCS;

namespace Hengine
{
    public struct DefaultRenderPassInfo
	{
		public Format colorFormat;
		public Format depthFormat;

        public DefaultRenderPassInfo(Format colorFormat, Format depthFormat)
        {
            this.colorFormat = colorFormat;
            this.depthFormat = depthFormat;
        }
    }

	public struct DefaultPipelineInfo
	{
		public Extent2D extent;

        public DefaultPipelineInfo(Extent2D extent)
        {
            this.extent = extent;
        }
    }

	public struct RenderPipelineOld
	{
		public void Dispose(VkContext context)
		{
			/*
			for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
			{
				context.vk.DestroySemaphore(context.device, imageAvailableSemaphores.Span[i], null);
				context.vk.DestroySemaphore(context.device, renderFinishedSemaphores.Span[i], null);
				context.vk.DestroyFence(context.device, inFlightFences.Span[i], null);
			}

			DisposeSwapChain();

			context.vk.DestroyDescriptorSetLayout(context.device, descriptorSetLayout, null);

			context.vk.DestroyBuffer(context.device, vertexBuffer, null);
			context.vk.FreeMemory(context.device, vertexBufferMemory, null);

			for (int i = 0; i < uniformBuffers.Length; i++)
			{
				context.vk.DestroyBuffer(context.device, uniformBuffers.Span[i], null);
			}

			for (int i = 0; i < uniformBuffersMemory.Length; i++)
			{
				context.vk.FreeMemory(context.device, uniformBuffersMemory.Span[i], null);
			}

			context.vk.DestroyCommandPool(context.device, commandPool, null);

			context.vk.DestroyDescriptorPool(context.device, descriptorPool, null);
			context.vk.DestroyDescriptorSetLayout(context.device, descriptorSetLayout, null);

			context.vk.DestroySampler(context.device, sampler, null);

			context.vk.DestroyImageView(context.device, textureImageView, null);
			context.vk.DestroyImage(context.device, texture, null);
			context.vk.FreeMemory(context.device, textureMemory, null);

			context.vk.DestroyImageView(context.device, depthImageView, null);
			context.vk.DestroyImage(context.device, depthImage, null);
			context.vk.FreeMemory(context.device, depthImageMemory, null);

			context.vk.TryGetInstanceExtension(context.instance, out KhrSurface khrSurface);
			khrSurface.DestroySurface(context.instance, surface, null);

			context.vk.DestroyDevice(context.device, null);
			context.vk.DestroyInstance(context.instance, null);
			*/
		}

        /*
		unsafe void DisposeSwapchain(VkContext context, CommandPool commandPool)
		{
			swapchain.Dispose(context);

			foreach (var frameBuffer in frameBuffers.Span)
			{
				context.vk.DestroyFramebuffer(context.device, frameBuffer, null);
			}

			foreach (var imageView in swapchainImages.Span)
			{
				context.vk.DestroyImageView(context.device, imageView, null);
			}

			foreach (var frame in framesInFlight.Span)
			{
				context.vk.FreeCommandBuffers(context.device, commandPool, [frame.commandBuffer]);
			}

			context.vk.DestroyPipeline(context.device, meshPipeline, null);
			context.vk.DestroyPipelineLayout(context.device, pipelineLayout, null);
			context.vk.DestroyRenderPass(context.device, skyboxRenderPass, null);
		}
        */

		/*
		public void RecreateSwapchain(VkContext context, SurfaceKHR surface, CommandPool commandPool)
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
