using Silk.NET.OpenAL;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Image = Silk.NET.Vulkan.Image;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Engine
{
	public unsafe struct Swapchain
	{
		SurfaceFormatKHR format;
		PresentModeKHR presentMode;
		uint imageCount;
		SurfaceTransformFlagsKHR transform;

		Extent2D extent;
		SwapchainKHR swapchain;

		uint graphicsQueueFamily;
		Queue graphicsQueue;

		uint presentQueueFamily;
		Queue presentQueue;

		Image depthImage;
		ImageView depthImageView;
		DeviceMemory depthImageMemory;

		public Swapchain(SurfaceFormatKHR format, PresentModeKHR presentMode, uint imageCount, SurfaceTransformFlagsKHR transform, Extent2D extent, SwapchainKHR swapchain, uint graphicsQueueFamily, Queue graphicsQueue, uint presentQueueFamily, Queue presentQueue, Image depthImage, ImageView depthImageView, DeviceMemory depthImageMemory)
		{
			this.format = format;
			this.presentMode = presentMode;
			this.imageCount = imageCount;
			this.transform = transform;
			this.extent = extent;
			this.swapchain = swapchain;
			this.graphicsQueueFamily = graphicsQueueFamily;
			this.graphicsQueue = graphicsQueue;
			this.presentQueueFamily = presentQueueFamily;
			this.presentQueue = presentQueue;
			this.depthImage = depthImage;
			this.depthImageView = depthImageView;
			this.depthImageMemory = depthImageMemory;
		}

		public void Dispose(VkContext context)
		{
			context.vk.DestroyImageView(context.device, depthImageView, null);
			context.vk.DestroyImage(context.device, depthImage, null);
			context.vk.FreeMemory(context.device, depthImageMemory, null);

			context.vk.TryGetDeviceExtension(context.instance, context.device, out KhrSwapchain khrSwapChain);
			khrSwapChain.DestroySwapchain(context.device, swapchain, null);
		}

		public Span<ImageView> GetImages(VkContext context, Span<ImageView> buff)
		{
			Span<Image> images = stackalloc Image[(int)imageCount];
			images = GetSwapChainImages(context, images, swapchain);

			VulkanHelper.CreateImageViews(context, buff, images, format.Format, ImageAspectFlags.ColorBit);
			return buff.Slice(0, (int)imageCount);
		}

		public Result AcquireNextImageIndex(VkContext context, Semaphore semaphore, out uint imageIndex)
		{
			context.vk.TryGetDeviceExtension(context.instance, context.device, out KhrSwapchain khrSwapChain);

			/*
			uint imageIndex = 0;
			var aquireResult = khrSwapChain.AcquireNextImage(context.device, swapchain, ulong.MaxValue, semaphore, default, ref imageIndex);
			if (aquireResult == Result.ErrorOutOfDateKhr || framebufferResized)
			{
				throw new Exception();
				framebufferResized = false;
				recreateSwapChain();
				return;
			}

			return imageIndex; ;
			*/
			imageIndex = 0;
			return khrSwapChain.AcquireNextImage(context.device, swapchain, ulong.MaxValue, semaphore, default, ref imageIndex);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public uint GetImageCount() => imageCount;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Extent2D GetExtent() => extent;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SurfaceFormatKHR GetSurfaceFormat() => format;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Queue GetGraphicsQueue() => graphicsQueue;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Queue GetPresentQueue() => presentQueue;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SwapchainKHR GetSwapchain() => swapchain;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ImageView GetDepthImage() => depthImageView;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Format GetDepthFormat(VkContext context) => VulkanHelper.FindSupportedFormat(context, [Format.D32Sfloat, Format.D32SfloatS8Uint, Format.D24UnormS8Uint], ImageTiling.Optimal, FormatFeatureFlags.DepthStencilAttachmentBit);

		public static Swapchain Create(VkContext context, SurfaceKHR surface, CommandPool commandPool)
		{
			var format = ChooseSwapSurfaceFormat(context, surface);
			var presentMode = ChooseSwapPresentMode(context, surface);
			var imageCount = ChooseImageCount(context, surface);
			var transform = ChooseSwapSurfaceTransform(context, surface);

			uint graphicsQueueFamily = VulkanHelper.GetGraphicsQueueFamily(context);
			uint presentQueueFamily = VulkanHelper.GetPresentQueueFamily(context, surface);

			Extent2D extent = ChooseSwapExtent(context, surface);
			SwapchainKHR swapchain = CreateSwapChain(context, surface, extent, graphicsQueueFamily, presentQueueFamily, format, presentMode, imageCount, transform);

			Queue graphicsQueue = VulkanHelper.GetQueue(context, graphicsQueueFamily);
			Queue presentQueue = VulkanHelper.GetQueue(context, presentQueueFamily);

			Image depthImage = VulkanHelper.CreateImage(context, extent, GetDepthFormat(context), ImageTiling.Optimal, ImageUsageFlags.DepthStencilAttachmentBit);
			DeviceMemory depthImageMemory = VulkanHelper.CreateMemory(context, depthImage, MemoryPropertyFlags.DeviceLocalBit);
			ImageView depthImageView = VulkanHelper.CreateImageView(context, depthImage, GetDepthFormat(context), ImageAspectFlags.DepthBit);

			VulkanHelper.TransitionImageLayout(context, commandPool, graphicsQueue, depthImage, GetDepthFormat(context), ImageLayout.Undefined, ImageLayout.DepthStencilAttachmentOptimal);

			return new(format, presentMode, imageCount, transform, extent, swapchain, graphicsQueueFamily, graphicsQueue, presentQueueFamily, presentQueue, depthImage, depthImageView, depthImageMemory);
		}

		static unsafe SwapchainKHR CreateSwapChain(VkContext context, SurfaceKHR surface, Extent2D extent, uint graphicsQ, uint presentQ, SurfaceFormatKHR format, PresentModeKHR presentMode, uint imageCount, SurfaceTransformFlagsKHR transform)
		{
			context.vk.TryGetDeviceExtension(context.instance, context.device, out KhrSwapchain khrSwapChain);

			SwapchainCreateInfoKHR createInfo = new();
			createInfo.SType = StructureType.SwapchainCreateInfoKhr;
			createInfo.Surface = surface;

			createInfo.MinImageCount = imageCount;
			createInfo.ImageFormat = format.Format;
			createInfo.ImageColorSpace = format.ColorSpace;
			createInfo.ImageExtent = extent;
			createInfo.ImageArrayLayers = 1;
			createInfo.ImageUsage = ImageUsageFlags.ColorAttachmentBit;

			if (graphicsQ != presentQ)
			{
				var familyIndicies = stackalloc uint[] { graphicsQ, presentQ };

				createInfo.ImageSharingMode = SharingMode.Concurrent;
				createInfo.QueueFamilyIndexCount = 2;
				createInfo.PQueueFamilyIndices = familyIndicies;
			}
			else
			{
				createInfo.ImageSharingMode = SharingMode.Exclusive;
			}

			createInfo.PreTransform = transform;
			createInfo.CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr;
			createInfo.PresentMode = presentMode;
			createInfo.Clipped = true;
			createInfo.OldSwapchain = default;

			var result = khrSwapChain.CreateSwapchain(context.device, createInfo, null, out SwapchainKHR swapchain);
			if (result != Result.Success)
				throw new Exception("Failed to create khrSwapchain");

			return swapchain;
		}

		static unsafe SurfaceFormatKHR ChooseSwapSurfaceFormat(VkContext context, SurfaceKHR surface)
		{
			context.vk.TryGetInstanceExtension(context.instance, out KhrSurface khrSurface);

			Span<SurfaceFormatKHR> surfaceFormats = stackalloc SurfaceFormatKHR[16];
			surfaceFormats = VulkanHelper.GetSurfaceFormats(surfaceFormats, khrSurface, context.physicalDevice, surface);

			foreach (var format in surfaceFormats)
			{
				if (format.Format == Format.B8G8R8A8Srgb && format.ColorSpace == ColorSpaceKHR.SpaceSrgbNonlinearKhr)
					return format;
			}

			// Fall back to first surface format
			return surfaceFormats[0];
		}

		static unsafe PresentModeKHR ChooseSwapPresentMode(VkContext context, SurfaceKHR surface)
		{
			context.vk.TryGetInstanceExtension(context.instance, out KhrSurface khrSurface);

			Span<PresentModeKHR> presentModees = stackalloc PresentModeKHR[16];
			presentModees = VulkanHelper.GetSurfacePresentModes(presentModees, khrSurface, context.physicalDevice, surface);

			foreach (var mode in presentModees)
			{
				if (mode == PresentModeKHR.MailboxKhr)
					return mode;
			}

			return PresentModeKHR.FifoKhr;
		}

		static Extent2D ChooseSwapExtent(VkContext context, SurfaceKHR surface)
		{
			context.vk.TryGetInstanceExtension(context.instance, out KhrSurface khrSurface);
			khrSurface.GetPhysicalDeviceSurfaceCapabilities(context.physicalDevice, surface, out SurfaceCapabilitiesKHR capabilities);

			if (capabilities.CurrentExtent.Width != uint.MaxValue)
				return capabilities.CurrentExtent;

			var frameBuffer = context.window.FramebufferSize;

			Extent2D actualExtent = new()
			{
				Width = (uint)frameBuffer.X,
				Height = (uint)frameBuffer.Y
			};

			actualExtent.Width = Math.Clamp(actualExtent.Width, capabilities.MinImageExtent.Width, capabilities.MaxImageExtent.Width);
			actualExtent.Height = Math.Clamp(actualExtent.Height, capabilities.MinImageExtent.Height, capabilities.MaxImageExtent.Height);

			return actualExtent;
		}

		static uint ChooseImageCount(VkContext context, SurfaceKHR surface)
		{
			context.vk.TryGetInstanceExtension(context.instance, out KhrSurface khrSurface);
			khrSurface.GetPhysicalDeviceSurfaceCapabilities(context.physicalDevice, surface, out SurfaceCapabilitiesKHR capabilities);

			if (capabilities.MaxImageCount == 0)
				return capabilities.MinImageCount + 2;

			return Math.Clamp(capabilities.MinImageCount + 2, capabilities.MinImageCount, capabilities.MaxImageCount);
		}

		static SurfaceTransformFlagsKHR ChooseSwapSurfaceTransform(VkContext context, SurfaceKHR surface)
		{
			context.vk.TryGetInstanceExtension(context.instance, out KhrSurface khrSurface);
			khrSurface.GetPhysicalDeviceSurfaceCapabilities(context.physicalDevice, surface, out SurfaceCapabilitiesKHR capabilities);

			return capabilities.SupportedTransforms;
		}

		static unsafe Span<Image> GetSwapChainImages(VkContext context, Span<Image> buff, SwapchainKHR swapchain)
		{
			context.vk.TryGetDeviceExtension(context.instance, context.device, out KhrSwapchain khrSwapChain);

			uint imageCount = 0;
			khrSwapChain.GetSwapchainImages(context.device, swapchain, ref imageCount, null);

			//Memory<Image> images = new Silk.NET.Vulkan.Image[imageCount];
			khrSwapChain.GetSwapchainImages(context.device, swapchain, &imageCount, buff);

			return buff.Slice(0, (int)imageCount);
		}
	}
}
