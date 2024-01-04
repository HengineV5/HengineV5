using Silk.NET.Core;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Image = Silk.NET.Vulkan.Image;
using Semaphore = Silk.NET.Vulkan.Semaphore;
using Buffer = Silk.NET.Vulkan.Buffer;
using System.Text;

namespace Engine
{
	public static class VulkanHelper
	{
		public static unsafe Image CreateImage(VkContext context, Extent2D extent, Format format, ImageTiling tiling, ImageUsageFlags usage)
		{
			ImageCreateInfo createInfo = new();
			createInfo.SType = StructureType.ImageCreateInfo;
			createInfo.ImageType = ImageType.Type2D;
			createInfo.Extent.Width = extent.Width;
			createInfo.Extent.Height = extent.Height;
			createInfo.Extent.Depth = 1;
			createInfo.MipLevels = 1;
			createInfo.ArrayLayers = 1;
			createInfo.Format = format;
			createInfo.Tiling = tiling;
			createInfo.InitialLayout = ImageLayout.Undefined;
			createInfo.Usage = usage;
			createInfo.SharingMode = SharingMode.Exclusive;
			createInfo.Samples = SampleCountFlags.Count1Bit;
			createInfo.Flags = ImageCreateFlags.None;

			var result = context.vk.CreateImage(context.device, createInfo, null, out Image image);
			if (result != Result.Success)
				throw new Exception("Failed to create vkImage");

			return image;
		}

		public static unsafe ImageView CreateImageView(VkContext context, Image image, Format format, ImageAspectFlags aspectMask)
		{
			ImageViewCreateInfo createInfo = new();
			createInfo.SType = StructureType.ImageViewCreateInfo;
			createInfo.Image = image;

			createInfo.ViewType = ImageViewType.Type2D;
			createInfo.Format = format;

			createInfo.Components.R = ComponentSwizzle.Identity;
			createInfo.Components.G = ComponentSwizzle.Identity;
			createInfo.Components.B = ComponentSwizzle.Identity;
			createInfo.Components.A = ComponentSwizzle.Identity;

			createInfo.SubresourceRange.AspectMask = aspectMask;
			createInfo.SubresourceRange.BaseMipLevel = 0;
			createInfo.SubresourceRange.LevelCount = 1;
			createInfo.SubresourceRange.BaseArrayLayer = 0;
			createInfo.SubresourceRange.LayerCount = 1;

			var result = context.vk.CreateImageView(context.device, createInfo, null, out ImageView imageView);
			if (result != Result.Success)
				throw new Exception("Failed to create vkImageView");

			return imageView;
		}

		public static unsafe void CreateImageViews(VkContext context, Span<ImageView> imageViews, Span<Image> images, Format format, ImageAspectFlags aspectMask)
		{
			for (int i = 0; i < imageViews.Length; i++)
			{
				imageViews[i] = CreateImageView(context, images[i], format, aspectMask);
			}
		}

		public static unsafe Framebuffer CreateFrameBuffer(VkContext context, Span<ImageView> attachments, RenderPass renderPass, Extent2D swapchainExtent)
		{
			FramebufferCreateInfo createInfo = new();
			createInfo.SType = StructureType.FramebufferCreateInfo;
			createInfo.RenderPass = renderPass;
			createInfo.AttachmentCount = 2;

			fixed (ImageView* attachmentsPtr = attachments)
				createInfo.PAttachments = attachmentsPtr;

			createInfo.Width = swapchainExtent.Width;
			createInfo.Height = swapchainExtent.Height;
			createInfo.Layers = 1;

			var result = context.vk.CreateFramebuffer(context.device, createInfo, null, out Framebuffer framebuffer);
			if (result != Result.Success)
				throw new Exception("Failed to create vkImageView");

			return framebuffer;
		}

		public static unsafe Sampler CreateSampler(VkContext context)
		{
			SamplerCreateInfo createInfo = new();
			createInfo.SType = StructureType.SamplerCreateInfo;
			createInfo.MagFilter = Filter.Linear;
			createInfo.MinFilter = Filter.Linear;
			createInfo.AddressModeU = SamplerAddressMode.Repeat;
			createInfo.AddressModeV = SamplerAddressMode.Repeat;
			createInfo.AddressModeW = SamplerAddressMode.Repeat;

			context.vk.GetPhysicalDeviceProperties(context.physicalDevice, out PhysicalDeviceProperties properties);

			createInfo.AnisotropyEnable = true;
			createInfo.MaxAnisotropy = properties.Limits.MaxSamplerAnisotropy;
			createInfo.BorderColor = BorderColor.FloatOpaqueBlack;
			createInfo.UnnormalizedCoordinates = false;
			createInfo.CompareEnable = false;
			createInfo.CompareOp = CompareOp.Always;
			createInfo.MipmapMode = SamplerMipmapMode.Linear;
			createInfo.MipLodBias = 0f;
			createInfo.MinLod = 0f;
			createInfo.MaxLod = 0f;

			var result = context.vk.CreateSampler(context.device, createInfo, null, out Sampler sampler);
			if (result != Result.Success)
				throw new Exception("Failed to create vkSampler");

			return sampler;
		}

		public static unsafe DeviceMemory CreateMemory(VkContext context, Image image, MemoryPropertyFlags properties)
		{
			context.vk.GetImageMemoryRequirements(context.device, image, out MemoryRequirements memRequirements);

			MemoryAllocateInfo allocInfo = new();
			allocInfo.SType = StructureType.MemoryAllocateInfo;
			allocInfo.AllocationSize = memRequirements.Size;
			allocInfo.MemoryTypeIndex = FindMemoryType(context, memRequirements.MemoryTypeBits, properties);

			var result = context.vk.AllocateMemory(context.device, allocInfo, null, out DeviceMemory bufferMemory);
			if (result != Result.Success)
				throw new Exception("Failed to allocate buffer memory");

			context.vk.BindImageMemory(context.device, image, bufferMemory, 0);

			return bufferMemory;
		}

		public static unsafe Semaphore CreateSemaphore(VkContext context)
		{
			SemaphoreCreateInfo createInfo = new();
			createInfo.SType = StructureType.SemaphoreCreateInfo;

			var result = context.vk.CreateSemaphore(context.device, createInfo, null, out Semaphore semaphore);
			if (result != Result.Success)
				throw new Exception("Failed to create vkSemaphore");

			return semaphore;
		}

		public static unsafe void CreateSemaphores(VkContext context, Span<Semaphore> semaphores)
		{
			SemaphoreCreateInfo createInfo = new();
			createInfo.SType = StructureType.SemaphoreCreateInfo;

			for (int i = 0; i < semaphores.Length; i++)
			{
				var result = context.vk.CreateSemaphore(context.device, createInfo, null, out semaphores[i]);
				if (result != Result.Success)
					throw new Exception("Failed to create vkSemaphore");
			}
		}

		public static unsafe Fence CreateFence(VkContext context, FenceCreateFlags flags)
		{
			FenceCreateInfo createInfo = new();
			createInfo.SType = StructureType.FenceCreateInfo;
			createInfo.Flags = flags;

			var result = context.vk.CreateFence(context.device, createInfo, null, out Fence fence);
			if (result != Result.Success)
				throw new Exception("Failed to create vkFence");

			return fence;
		}

		public static unsafe void CreateFences(VkContext context, FenceCreateFlags flags, Span<Fence> fences)
		{
			FenceCreateInfo createInfo = new();
			createInfo.SType = StructureType.FenceCreateInfo;
			createInfo.Flags = flags;

			for (int i = 0; i < fences.Length; i++)
			{
				var result = context.vk.CreateFence(context.device, createInfo, null, out fences[i]);
				if (result != Result.Success)
					throw new Exception("Failed to create vkFence");
			}
		}

		public static unsafe Buffer CreateBuffer<T>(VkContext context, BufferUsageFlags bufferUsage, uint dataCount) where T : unmanaged
		{
			return CreateBuffer(context, bufferUsage, (uint)sizeof(T) * dataCount);
		}

		public static unsafe Buffer CreateBuffer(VkContext context, BufferUsageFlags bufferUsage, uint size)
		{
			BufferCreateInfo createInfo = new();
			createInfo.SType = StructureType.BufferCreateInfo;
			createInfo.Size = size;
			createInfo.Usage = bufferUsage;
			createInfo.SharingMode = SharingMode.Exclusive;
			createInfo.Flags = BufferCreateFlags.None;

			var result = context.vk.CreateBuffer(context.device, createInfo, null, out Buffer buffer);
			if (result != Result.Success)
				throw new Exception("Failed to create vkBuffer");

			return buffer;
		}

		public static Memory<Buffer> CreateBuffers<T>(VkContext context, BufferUsageFlags bufferUsage, uint dataCount, uint bufferCount) where T : unmanaged
		{
			Memory<Buffer> buffers = new Buffer[bufferCount];
			for (int i = 0; i < buffers.Length; i++)
			{
				buffers.Span[i] = CreateBuffer<T>(context, bufferUsage, dataCount);
			}

			return buffers;
		}

		public static unsafe DeviceMemory CreateBufferMemory(VkContext context, Buffer buffer, MemoryPropertyFlags properties)
		{
			context.vk.GetBufferMemoryRequirements(context.device, buffer, out MemoryRequirements memRequirements);

			MemoryAllocateInfo allocInfo = new();
			allocInfo.SType = StructureType.MemoryAllocateInfo;
			allocInfo.AllocationSize = memRequirements.Size;
			allocInfo.MemoryTypeIndex = FindMemoryType(context, memRequirements.MemoryTypeBits, properties);

			var result = context.vk.AllocateMemory(context.device, allocInfo, null, out DeviceMemory bufferMemory);
			if (result != Result.Success)
				throw new Exception("Failed to allocate buffer memory");

			context.vk.BindBufferMemory(context.device, buffer, bufferMemory, 0);

			return bufferMemory;
		}

		public static Memory<DeviceMemory> CreateBufferMemories(VkContext context, Span<Buffer> buffers, MemoryPropertyFlags properties)
		{
			Memory<DeviceMemory> bufferMemories = new DeviceMemory[buffers.Length];
			for (int i = 0; i < buffers.Length; i++)
			{
				bufferMemories.Span[i] = CreateBufferMemory(context, buffers[i], properties);
			}

			return bufferMemories;
		}

		public static unsafe DescriptorPool CreateDescriptorPool(VkContext context, uint size)
		{
			DescriptorPoolSize uniformSize = new();
			uniformSize.Type = DescriptorType.UniformBuffer;
			uniformSize.DescriptorCount = size;

			DescriptorPoolSize samplerSize = new();
			samplerSize.Type = DescriptorType.CombinedImageSampler;
			samplerSize.DescriptorCount = size;

			DescriptorPoolSize* bindingsPtr = stackalloc DescriptorPoolSize[2];
			bindingsPtr[0] = uniformSize;
			bindingsPtr[1] = samplerSize;

			DescriptorPoolCreateInfo createInfo = new();
			createInfo.SType = StructureType.DescriptorPoolCreateInfo;
			createInfo.PoolSizeCount = 2;
			createInfo.PPoolSizes = bindingsPtr;
			createInfo.MaxSets = size;

			var result = context.vk.CreateDescriptorPool(context.device, createInfo, null, out DescriptorPool descriptorPool);
			if (result != Result.Success)
				throw new Exception("Failed to create vkDescriptorPool");

			return descriptorPool;
		}

		public static unsafe CommandBuffer CreateCommandBuffer(VkContext context, CommandPool commandPool)
		{
			CommandBufferAllocateInfo allocInfo = new();
			allocInfo.SType = StructureType.CommandBufferAllocateInfo;
			allocInfo.CommandPool = commandPool;
			allocInfo.Level = CommandBufferLevel.Primary;
			allocInfo.CommandBufferCount = 1;

			var result = context.vk.AllocateCommandBuffers(context.device, allocInfo, out CommandBuffer commandBuffer);
			if (result != Result.Success)
				throw new Exception("Failed to create vkCommandBuffer");

			return commandBuffer;
		}

		public static unsafe Memory<CommandBuffer> CreateCommandBuffers(VkContext context, CommandPool commandPool, uint count)
		{
			Memory<CommandBuffer> commandBuffers = new CommandBuffer[count];

			CommandBufferAllocateInfo allocInfo = new();
			allocInfo.SType = StructureType.CommandBufferAllocateInfo;
			allocInfo.CommandPool = commandPool;
			allocInfo.Level = CommandBufferLevel.Primary;
			allocInfo.CommandBufferCount = (uint)commandBuffers.Length;

			fixed (CommandBuffer* commandBufferPtr = commandBuffers.Span)
			{
				var result = context.vk.AllocateCommandBuffers(context.device, allocInfo, commandBufferPtr);
				if (result != Result.Success)
					throw new Exception("Failed to create vkCommandBuffer");
			}

			return commandBuffers;
		}

		public static unsafe CommandPool CreateCommandPool(VkContext context, uint graphicsQueueFamily)
		{
			CommandPoolCreateInfo createInfo = new();
			createInfo.SType = StructureType.CommandPoolCreateInfo;
			createInfo.Flags = CommandPoolCreateFlags.ResetCommandBufferBit;
			createInfo.QueueFamilyIndex = graphicsQueueFamily;

			var result = context.vk.CreateCommandPool(context.device, createInfo, null, out CommandPool commandPool);
			if (result != Result.Success)
				throw new Exception("Failed to create vkCommandPool");

			return commandPool;
		}

		public static unsafe void MapBufferMemory<T>(VkContext context, Buffer buffer, DeviceMemory bufferMemory, T data) where T : unmanaged
		{
			context.vk.GetBufferMemoryRequirements(context.device, buffer, out MemoryRequirements memRequirements);

			void* dataPtr;
			context.vk.MapMemory(context.device, bufferMemory, 0, memRequirements.Size, 0, &dataPtr);
			((T*)dataPtr)[0] = data;
			context.vk.UnmapMemory(context.device, bufferMemory);
		}

		public static unsafe void MapBufferMemory<T>(VkContext context, Buffer buffer, DeviceMemory bufferMemory, Span<T> data) where T : unmanaged
		{
			context.vk.GetBufferMemoryRequirements(context.device, buffer, out MemoryRequirements memRequirements);

			void* dataPtr;
			context.vk.MapMemory(context.device, bufferMemory, 0, memRequirements.Size, 0, &dataPtr);
			data.CopyTo(new Span<T>(dataPtr, data.Length));
			context.vk.UnmapMemory(context.device, bufferMemory);
		}

		public static Format FindSupportedFormat(VkContext context, Span<Format> formats, ImageTiling tiling, FormatFeatureFlags features)
		{
			for (int i = 0; i < formats.Length; i++)
			{
				context.vk.GetPhysicalDeviceFormatProperties(context.physicalDevice, formats[i], out FormatProperties formatProperties);

				if (tiling == ImageTiling.Linear && formatProperties.LinearTilingFeatures.HasFlag(features))
					return formats[i];

				if (tiling == ImageTiling.Optimal && formatProperties.OptimalTilingFeatures.HasFlag(features))
					return formats[i];
			}

			throw new Exception("Unable to find supported  format");
		}

		public static uint FindMemoryType(VkContext context, uint typeFilter, MemoryPropertyFlags properties)
		{
			context.vk.GetPhysicalDeviceMemoryProperties(context.physicalDevice, out PhysicalDeviceMemoryProperties memoryProperties);

			for (uint i = 0; i < memoryProperties.MemoryTypeCount; i++)
			{
				var memoryProperty = memoryProperties.MemoryTypes.AsSpan()[(int)i];

				if (((int)typeFilter & (1 << (int)i)) != 0 && memoryProperty.PropertyFlags.HasFlag(properties))
					return i;
			}

			throw new Exception("Failed to find suitable memory type.");
		}

		public static void WaitForFence(VkContext context, Fence fence, ulong timeout = ulong.MaxValue)
		{
			context.vk.WaitForFences(context.device, [fence], true, timeout);
		}

		public unsafe static void QueueSubmitCommands(VkContext context, Queue queue, CommandBuffer command, Semaphore semaphore, Fence fence, PipelineStageFlags stageFlags)
		{
			SubmitInfo submitInfo = new();
			submitInfo.SType = StructureType.SubmitInfo;
			submitInfo.WaitSemaphoreCount = 1;
			submitInfo.PWaitSemaphores = &semaphore;
			submitInfo.PWaitDstStageMask = &stageFlags;

			submitInfo.CommandBufferCount = 1;
			submitInfo.PCommandBuffers = &command;

			submitInfo.SignalSemaphoreCount = 1;
			submitInfo.PSignalSemaphores = &semaphore;

			var result = context.vk.QueueSubmit(queue, 1, submitInfo, fence);
			if (result != Result.Success)
				throw new Exception("Failed to submit draw command buffer!");
		}

		public static unsafe void QueuePresent(VkContext context, Queue queue, SwapchainKHR swapchain, uint imageIndex, Semaphore semaphore)
		{
			PresentInfoKHR presentInfo = new();
			presentInfo.SType = StructureType.PresentInfoKhr;
			presentInfo.WaitSemaphoreCount = 1;
			presentInfo.PWaitSemaphores = &semaphore;

			Span<SwapchainKHR> swapchains = [swapchain];

			presentInfo.SwapchainCount = 1;
			fixed (SwapchainKHR* swapchainPtr = swapchains)
				presentInfo.PSwapchains = swapchainPtr;

			presentInfo.PImageIndices = &imageIndex;
			presentInfo.PResults = null;

			var presentResult = context.swapchain.QueuePresent(queue, presentInfo);
			if (presentResult == Result.ErrorOutOfDateKhr || presentResult == Result.SuboptimalKhr)
			{
				throw new Exception();
				//recreateSwapChain();
			}
		}

		public static unsafe CommandBuffer BeginSingleShotCommands(VkContext context, CommandPool commandPool)
		{
			CommandBuffer commandBuffer = CreateCommandBuffer(context, commandPool);

			CommandBufferBeginInfo beginInfo = new();
			beginInfo.SType = StructureType.CommandBufferBeginInfo;
			beginInfo.Flags = CommandBufferUsageFlags.OneTimeSubmitBit;

			context.vk.BeginCommandBuffer(commandBuffer, beginInfo);

			return commandBuffer;
		}

		public static unsafe void EndSingleShotCommands(VkContext context, CommandBuffer commandBuffer, CommandPool commandPool, Queue queue)
		{
			context.vk.EndCommandBuffer(commandBuffer);

			SubmitInfo submitInfo = new();
			submitInfo.SType = StructureType.SubmitInfo;
			submitInfo.CommandBufferCount = 1;
			submitInfo.PCommandBuffers = &commandBuffer;

			context.vk.QueueSubmit(queue, 1, &submitInfo, default);
			context.vk.QueueWaitIdle(queue);

			context.vk.FreeCommandBuffers(context.device, commandPool, 1, commandBuffer);
		}

		public static unsafe void CopyBuffer(VkContext context, CommandPool commandPool, Queue queue, Silk.NET.Vulkan.Buffer srcBuffer, Silk.NET.Vulkan.Buffer dstBuffer, ulong size)
		{
			CommandBuffer commandBuffer = BeginSingleShotCommands(context, commandPool);

			BufferCopy copyRegion = new();
			copyRegion.SrcOffset = 0;
			copyRegion.DstOffset = 0;
			copyRegion.Size = size;

			context.vk.CmdCopyBuffer(commandBuffer, srcBuffer, dstBuffer, 1, copyRegion);

			EndSingleShotCommands(context, commandBuffer, commandPool, queue);
		}

		public static unsafe void CopyBuffer(VkContext context, CommandPool commandPool, Queue queue, Silk.NET.Vulkan.Buffer srcBuffer, Silk.NET.Vulkan.Image dstImage, uint width, uint height)
		{
			CommandBuffer commandBuffer = BeginSingleShotCommands(context, commandPool);

			BufferImageCopy copyRegion = new();
			copyRegion.BufferOffset = 0;
			copyRegion.BufferRowLength = 0;
			copyRegion.BufferImageHeight = 0;

			copyRegion.ImageSubresource.AspectMask = ImageAspectFlags.ColorBit;
			copyRegion.ImageSubresource.MipLevel = 0;
			copyRegion.ImageSubresource.BaseArrayLayer = 0;
			copyRegion.ImageSubresource.LayerCount = 1;

			copyRegion.ImageOffset = new(0, 0, 0);
			copyRegion.ImageExtent = new(width, height, 1);

			context.vk.CmdCopyBufferToImage(commandBuffer, srcBuffer, dstImage, ImageLayout.TransferDstOptimal, 1, copyRegion);

			EndSingleShotCommands(context, commandBuffer, commandPool, queue);
		}

		public static unsafe void TransitionImageLayout(VkContext context, CommandPool commandPool, Queue queue, Silk.NET.Vulkan.Image image, Format format, ImageLayout oldLayout, ImageLayout newLayout)
		{
			CommandBuffer commandBuffer = BeginSingleShotCommands(context, commandPool);

			ImageMemoryBarrier barrier = new();
			barrier.SType = StructureType.ImageMemoryBarrier;
			barrier.OldLayout = oldLayout;
			barrier.NewLayout = newLayout;
			barrier.SrcQueueFamilyIndex = Vk.QueueFamilyIgnored;
			barrier.DstQueueFamilyIndex = Vk.QueueFamilyIgnored;
			barrier.Image = image;
			barrier.SubresourceRange.AspectMask = ImageAspectFlags.ColorBit;
			barrier.SubresourceRange.BaseMipLevel = 0;
			barrier.SubresourceRange.LevelCount = 1;
			barrier.SubresourceRange.BaseArrayLayer = 0;
			barrier.SubresourceRange.LayerCount = 1;

			if (newLayout == ImageLayout.DepthStencilAttachmentOptimal)
			{
				barrier.SubresourceRange.AspectMask = ImageAspectFlags.DepthBit;

				if (format.HasStencilComponent())
					barrier.SubresourceRange.AspectMask |= ImageAspectFlags.StencilBit;
			}

			PipelineStageFlags sourceStage = PipelineStageFlags.None;
			PipelineStageFlags destinationStage = PipelineStageFlags.None;

			if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.TransferDstOptimal)
			{
				barrier.SrcAccessMask = AccessFlags.None;
				barrier.DstAccessMask = AccessFlags.TransferWriteBit;

				sourceStage = PipelineStageFlags.TopOfPipeBit;
				destinationStage = PipelineStageFlags.TransferBit;
			}
			else if (oldLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.ShaderReadOnlyOptimal)
			{
				barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
				barrier.DstAccessMask = AccessFlags.ShaderReadBit;

				sourceStage = PipelineStageFlags.TransferBit;
				destinationStage = PipelineStageFlags.FragmentShaderBit;
			}
			else if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.DepthStencilAttachmentOptimal)
			{
				barrier.SrcAccessMask = AccessFlags.None;
				barrier.DstAccessMask = AccessFlags.DepthStencilAttachmentReadBit | AccessFlags.DepthStencilAttachmentWriteBit;

				sourceStage = PipelineStageFlags.TopOfPipeBit;
				destinationStage = PipelineStageFlags.EarlyFragmentTestsBit;
			}
			else
			{
				throw new Exception("Invlaid layout transition!");
			}

			context.vk.CmdPipelineBarrier(commandBuffer, sourceStage, destinationStage, DependencyFlags.None, 0, null, 0, null, 1, &barrier);

			EndSingleShotCommands(context, commandBuffer, commandPool, queue);
		}

		public static unsafe uint GetGraphicsQueueFamily(VkContext context)
		{
			Span<QueueFamilyProperties> qFamilies = stackalloc QueueFamilyProperties[16];
			qFamilies = GetQueueFamilies(context, qFamilies, context.physicalDevice);

			for (uint i = 0; i < qFamilies.Length; i++)
			{
				if (qFamilies[(int)i].QueueFlags.HasFlag(QueueFlags.GraphicsBit))
					return i;
			}

			throw new Exception("Queue with graphics bit not found.");
		}

		public static unsafe uint GetPresentQueueFamily(VkContext context, SurfaceKHR surface)
		{
			Span<QueueFamilyProperties> qFamilies = stackalloc QueueFamilyProperties[16];
			qFamilies = GetQueueFamilies(context, qFamilies, context.physicalDevice);

			for (uint i = 0; i < qFamilies.Length; i++)
			{
				context.surface.GetPhysicalDeviceSurfaceSupport(context.physicalDevice, i, surface, out Bool32 supported);

				if (supported.Value == 1)
					return i;
			}

			throw new Exception("Queue with graphics bit not found.");
		}

		public static unsafe Span<QueueFamilyProperties> GetQueueFamilies(VkContext context, Span<QueueFamilyProperties> buff, PhysicalDevice physicalDevice)
		{
			uint qFamilyCount = 0;
			context.vk.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, ref qFamilyCount, null);

			fixed (QueueFamilyProperties* buffPtr = buff)
			{
				context.vk.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, ref qFamilyCount, buffPtr);
			}

			return buff.Slice(0, (int)qFamilyCount);
		}

		public static unsafe Queue GetQueue(VkContext context, uint familyIndex)
		{
			context.vk.GetDeviceQueue(context.device, familyIndex, 0, out Queue queue);

			return queue;
		}

		public static unsafe Span<SurfaceFormatKHR> GetSurfaceFormats(Span<SurfaceFormatKHR> buff, KhrSurface khrSurface, PhysicalDevice physicalDevice, SurfaceKHR surface)
		{
			uint formatCount = 0;
			khrSurface.GetPhysicalDeviceSurfaceFormats(physicalDevice, surface, ref formatCount, null);
			khrSurface.GetPhysicalDeviceSurfaceFormats(physicalDevice, surface, &formatCount, buff);

			return buff.Slice(0, (int)formatCount);
		}

		public static unsafe Span<PresentModeKHR> GetSurfacePresentModes(Span<PresentModeKHR> buff, KhrSurface khrSurface, PhysicalDevice physicalDevice, SurfaceKHR surface)
		{
			uint presentModeCount = 0;
			khrSurface.GetPhysicalDeviceSurfacePresentModes(physicalDevice, surface, ref presentModeCount, null);
			khrSurface.GetPhysicalDeviceSurfacePresentModes(physicalDevice, surface, &presentModeCount, buff);

			return buff.Slice(0, (int)presentModeCount);
		}

		public static unsafe void PrintExtensions(VkContext context)
		{
			uint instanceExtensionCount = 0;
			context.vk.EnumerateInstanceExtensionProperties((byte*)null, &instanceExtensionCount, null);

			Span<ExtensionProperties> extensionProps = stackalloc ExtensionProperties[(int)instanceExtensionCount];
			context.vk.EnumerateInstanceExtensionProperties((byte*)null, &instanceExtensionCount, extensionProps);

			Console.WriteLine("Instance extensions:");
			foreach (var item in extensionProps)
			{
				Console.WriteLine($"\t{Encoding.UTF8.GetString(item.ExtensionName, 256)}");
			}

			Console.WriteLine();

			uint deviceExtensionCount = 0;
			context.vk.EnumerateDeviceExtensionProperties(context.physicalDevice, (byte*)null, &deviceExtensionCount, null);

			Span<ExtensionProperties> deviceProps = stackalloc ExtensionProperties[(int)deviceExtensionCount];
			context.vk.EnumerateDeviceExtensionProperties(context.physicalDevice, (byte*)null, &deviceExtensionCount, deviceProps);

			Console.WriteLine("Device extensions:");
			foreach (var item in deviceProps)
			{
				Console.WriteLine($"\t{Encoding.UTF8.GetString(item.ExtensionName, 256)}");
			}
		}

		public static unsafe void PrintSurfaceCapabilities(VkContext context, SurfaceKHR surface)
		{
			context.surface.GetPhysicalDeviceSurfaceCapabilities(context.physicalDevice, surface, out SurfaceCapabilitiesKHR capabilities);
			Console.WriteLine($"Extent: {capabilities.CurrentExtent.Width}, {capabilities.CurrentExtent.Height}");
			Console.WriteLine($"Image Count: {capabilities.MinImageCount}-{capabilities.MaxImageCount}");

			Span<SurfaceFormatKHR> surfaceFormats = stackalloc SurfaceFormatKHR[16];
			surfaceFormats = VulkanHelper.GetSurfaceFormats(surfaceFormats, context.surface, context.physicalDevice, surface);

			Span<PresentModeKHR> presentModees = stackalloc PresentModeKHR[16];
			presentModees = VulkanHelper.GetSurfacePresentModes(presentModees, context.surface, context.physicalDevice, surface);

			Console.WriteLine("Surface formats:");
			foreach (var item in surfaceFormats)
			{
				Console.WriteLine($"\tFormat: {item.Format}, ColorSpace: {item.ColorSpace}");
			}

			Console.WriteLine("Present modes:");
			foreach (var item in presentModees)
			{
				Console.WriteLine($"\t{item}");
			}
		}

		public static unsafe void PrintQueueFamilies(VkContext context)
		{
			Span<QueueFamilyProperties> qFamilies = stackalloc QueueFamilyProperties[16];
			qFamilies = VulkanHelper.GetQueueFamilies(context, qFamilies, context.physicalDevice);

			Console.WriteLine("Queue families:");
			foreach (var family in qFamilies)
			{
				Console.WriteLine($"\tCount: {family.QueueCount}, Flags: {family.QueueFlags}");
			}
		}

		public static unsafe void PrintValidationLayers(VkContext context)
		{
			uint layerCount = 0;
			context.vk.EnumerateInstanceLayerProperties(ref layerCount, null);

			Span<LayerProperties> layerProperties = stackalloc LayerProperties[(int)layerCount];
			context.vk.EnumerateInstanceLayerProperties(ref layerCount, ref layerProperties[0]);

			Console.WriteLine("Validation Layers:");
			foreach (var layer in layerProperties)
			{
				var layerName = Encoding.UTF8.GetString(layer.LayerName, 256).Trim().Replace("\0", "");

				Console.WriteLine($"\t{layerName}");
			}
		}

		public static void PrintMemoryTypes(VkContext context)
		{
			context.vk.GetPhysicalDeviceMemoryProperties(context.physicalDevice, out PhysicalDeviceMemoryProperties memoryProperties);
			Console.WriteLine("Memory Types:");
			foreach (var layer in memoryProperties.MemoryTypes.AsSpan())
			{
				if (layer.PropertyFlags == MemoryPropertyFlags.None)
					continue;

				Console.WriteLine($"\t{layer.HeapIndex}: {layer.PropertyFlags}");
			}
		}
	}
}
