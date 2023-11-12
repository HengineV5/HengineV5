using EnCS.Attributes;
using Engine.Components;
using Engine.Graphics;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using System.Buffers;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Engine
{
	struct UniformBufferObject
	{
		public Matrix4x4 translation;
		public Matrix4x4 rotation;
		public Matrix4x4 scale;
		public Matrix4x4 view;
		public Matrix4x4 proj;
	}

	unsafe struct MappedMemory<T> where T : unmanaged
	{
		public ref T Value
		{
			get
			{
				return ref data[0];
			}
		}

		T* data;

        public MappedMemory(T* data)
        {
			this.data = data;
        }
    }

	[System]
	public partial class VulkanRenderSystem
	{
		const int MAX_FRAMES_IN_FLIGHT = 3;

		/*
		static Memory<Vertex> verticies = new Vertex[]
		{
			new(new (-0.5f, -0.5f, 0), new (1.0f, 0.0f, 0.0f), new(1.0f, 0.0f)),
			new(new (0.5f, -0.5f, 0), new (0.0f, 1.0f, 0.0f), new(0.0f, 0.0f)),
			new(new (0.5f, 0.5f, 0), new (0.0f, 0.0f, 1.0f), new(0.0f, 1.0f)),
			new(new (-0.5f, 0.5f, 0), new (1.0f, 1.0f, 1.0f), new(1.0f, 1.0f)),

			new(new (-0.5f, -0.5f, -0.5f), new (1.0f, 0.0f, 0.0f), new(1.0f, 0.0f)),
			new(new (0.5f, -0.5f, -0.5f), new (0.0f, 1.0f, 0.0f), new(0.0f, 0.0f)),
			new(new (0.5f, 0.5f, -0.5f), new (0.0f, 0.0f, 1.0f), new(0.0f, 1.0f)),
			new(new (-0.5f, 0.5f, -0.5f), new (1.0f, 1.0f, 1.0f), new(1.0f, 1.0f))
		};

		static Memory<ushort> indicies = new ushort[]
		{
			0, 1, 2, 2, 3, 0,
			4, 5, 6, 6, 7, 4
		};
		*/

		uint indiciesLength;

		IWindow window;
		//Vk vk;
		VkContext context;

		/*
		Instance instance;
		PhysicalDevice physicalDevice;

		Device device;
		*/

		Queue graphicsQueue;
		Queue presentQueue;
		SurfaceKHR surface;
		SwapchainKHR swapchain;
		RenderPass renderPass;
		DescriptorSetLayout descriptorSetLayout;
		PipelineLayout pipelineLayout;
		Pipeline pipeline;
		CommandPool commandPool;
		DescriptorPool descriptorPool;

		Silk.NET.Vulkan.Image texture;
		DeviceMemory textureMemory;
		ImageView textureImageView;

		Silk.NET.Vulkan.Image depthImage;
		DeviceMemory depthImageMemory;
		ImageView depthImageView;

		Sampler sampler;

		Memory<CommandBuffer> commandBuffers;

		Silk.NET.Vulkan.Buffer vertexBuffer;
		DeviceMemory vertexBufferMemory;
		Silk.NET.Vulkan.Buffer indexBuffer;
		DeviceMemory indexBufferMemory;

		Memory<Silk.NET.Vulkan.Buffer> uniformBuffers;
		Memory<DeviceMemory> uniformBuffersMemory;
		Memory<MappedMemory<UniformBufferObject>> uniformBufferObjects;

		Memory<Silk.NET.Vulkan.Image> images;
		Memory<ImageView> imageViews;
		Memory<Framebuffer> frameBuffers;

		Memory<Silk.NET.Vulkan.Semaphore> imageAvailableSemaphores;
		Memory<Silk.NET.Vulkan.Semaphore> renderFinishedSemaphores;
		Memory<Fence> inFlightFences;

		Memory<DescriptorSet> descriptorSets;

		/*
		string[] validationLayers = [
			"VK_LAYER_KHRONOS_validation"
		];

		string[] deviceExtensions = [
			KhrSwapchain.ExtensionName
		];
		*/

		int currentFrame = 0;
		bool framebufferResized = false;

		public VulkanRenderSystem(VkContext context, IWindow window)
		{
			this.context = context;
			this.window = window;
        }

		public void Init()
		{
			var meshBall = Mesh.LoadOBJ("Models/Viking.obj");
			Memory<Vertex> verticies = meshBall.verticies;
			Memory<ushort> indicies = new ushort[meshBall.indicies.Length];
			for (int i = 0; i < meshBall.indicies.Length; i++)
			{
				indicies.Span[i] = (ushort)meshBall.indicies[i];
			}

			indiciesLength = (uint)indicies.Length;

			window.FramebufferResize += Window_Resize;

			//instance = CreateInstance(vk, window, validationLayers);
			//physicalDevice = PickPhysicalDevice(vk, instance);

			Format depthFormat = FindSupportedFormat(context, [Format.D32Sfloat, Format.D32SfloatS8Uint, Format.D24UnormS8Uint], ImageTiling.Optimal, FormatFeatureFlags.DepthStencilAttachmentBit);

			var graphicsQueueFamily = GetGraphicsQueueFamily(context);
			//device = CreateLogicalDevice(vk, physicalDevice, graphicsQueueFamily, validationLayers, deviceExtensions);
			graphicsQueue = GetQueue(context, graphicsQueueFamily);
			surface = CreateSurface(context);
			swapchain = CreateSwapChain(context, surface, graphicsQueueFamily);

			var presentQueueFamily = GetPresentQueueFamily(context, surface);
			presentQueue = GetQueue(context, presentQueueFamily);

			var extent = ChooseSwapExtent(context, surface);
			images = GetSwapChainImages(context, swapchain);
			imageViews = CreateImageViews(context, images.Span, surface, ImageAspectFlags.ColorBit);

			commandPool = CreateCommandPool(context, graphicsQueueFamily);
			commandBuffers = CreateCommandBuffers(context, commandPool, MAX_FRAMES_IN_FLIGHT);

			depthImage = CreateTextureImage(context, extent, depthFormat, ImageTiling.Optimal, ImageUsageFlags.DepthStencilAttachmentBit);
			depthImageMemory = CreateImageMemory(context, depthImage, MemoryPropertyFlags.DeviceLocalBit);
			depthImageView = CreateImageView(context, depthImage, depthFormat, ImageAspectFlags.DepthBit);

			TransitionImageLayout(context, commandPool, graphicsQueue, depthImage, depthFormat, ImageLayout.Undefined, ImageLayout.DepthStencilAttachmentOptimal);

			renderPass = CreateRenderPass(context, surface, depthFormat);
			frameBuffers = CreateFrameBuffers(context, imageViews.Span, depthImageView, renderPass, extent);

			descriptorSetLayout = CreateDescriptorSetLayout(context);
			pipelineLayout = CreatePipelineLayout(context, descriptorSetLayout);
			pipeline = CreateGraphicsPipeline(context, extent, pipelineLayout, renderPass);

			vertexBuffer = CreateBuffer<Vertex>(context, BufferUsageFlags.TransferDstBit | BufferUsageFlags.VertexBufferBit, (uint)verticies.Length);
			vertexBufferMemory = CreateBufferMemory(context, vertexBuffer, MemoryPropertyFlags.DeviceLocalBit);
			indexBuffer = CreateBuffer<ushort>(context, BufferUsageFlags.TransferDstBit | BufferUsageFlags.IndexBufferBit, (uint)indicies.Length);
			indexBufferMemory = CreateBufferMemory(context, indexBuffer, MemoryPropertyFlags.DeviceLocalBit);

			uniformBuffers = CreateBuffers<UniformBufferObject>(context, BufferUsageFlags.UniformBufferBit, 1, MAX_FRAMES_IN_FLIGHT);
			uniformBuffersMemory = CreateBufferMemories(context, uniformBuffers.Span, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

			uniformBufferObjects = new MappedMemory<UniformBufferObject>[MAX_FRAMES_IN_FLIGHT];
			unsafe
			{
				for (int i = 0; i < uniformBufferObjects.Length; i++)
				{
					void* dataPtr;
					context.vk.MapMemory(context.device, uniformBuffersMemory.Span[i], 0, (ulong)sizeof(UniformBufferObject), 0, &dataPtr);

					uniformBufferObjects.Span[i] = new((UniformBufferObject*)dataPtr);
				}
			}

			Silk.NET.Vulkan.Buffer stagingBuffer = CreateBuffer<Vertex>(context, BufferUsageFlags.TransferSrcBit, (uint)verticies.Length);
			DeviceMemory stagingBufferMemory = CreateBufferMemory(context, stagingBuffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

			MapBufferMemory(context, stagingBuffer, stagingBufferMemory, verticies.Span);
			CopyBuffer(context, commandPool, graphicsQueue, stagingBuffer, vertexBuffer, (uint)verticies.Span.Length * Vertex.SizeInBytes);

			MapBufferMemory(context, stagingBuffer, stagingBufferMemory, indicies.Span);
			CopyBuffer(context, commandPool, graphicsQueue, stagingBuffer, indexBuffer, (uint)indicies.Span.Length * sizeof(ushort));

			unsafe
			{
				context.vk.DestroyBuffer(context.device, stagingBuffer, null);
				context.vk.FreeMemory(context.device, stagingBufferMemory, null);
			}

			imageAvailableSemaphores = CreateSemaphores(context);
			renderFinishedSemaphores = CreateSemaphores(context);
			inFlightFences = CreateFences(context, FenceCreateFlags.SignaledBit);

			sampler = CreateTextureSampler(context);

			using var img = SixLabors.ImageSharp.Image.Load<Rgba32>("Images/viking_room.png");
			int imageSize = img.Width * img.Height * img.PixelType.BitsPerPixel / 8;

			using var buff = MemoryPool<byte>.Shared.Rent(imageSize);
			img.CopyPixelDataTo(buff.Memory.Span);

			stagingBuffer = CreateBuffer<byte>(context, BufferUsageFlags.TransferSrcBit, (uint)imageSize);
			stagingBufferMemory = CreateBufferMemory(context, stagingBuffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

			MapBufferMemory(context, stagingBuffer, stagingBufferMemory, buff.Memory.Span);

			texture = CreateTextureImage(context, new((uint)img.Width, (uint)img.Height), Format.R8G8B8A8Srgb, ImageTiling.Optimal, ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit);
			textureMemory = CreateImageMemory(context, texture, MemoryPropertyFlags.DeviceLocalBit);
			textureImageView = CreateImageView(context, texture, Format.R8G8B8A8Srgb, ImageAspectFlags.ColorBit);

			TransitionImageLayout(context, commandPool, graphicsQueue, texture, Format.R8G8B8A8Srgb, ImageLayout.Undefined, ImageLayout.TransferDstOptimal);
			CopyBuffer(context, commandPool, graphicsQueue, stagingBuffer, texture, (uint)img.Width, (uint)img.Height);
			TransitionImageLayout(context, commandPool, graphicsQueue, texture, Format.R8G8B8A8Srgb, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);

			unsafe
			{
				context.vk.DestroyBuffer(context.device, stagingBuffer, null);
				context.vk.FreeMemory(context.device, stagingBufferMemory, null);
			}

			descriptorPool = CreateDescriptorPool(context, MAX_FRAMES_IN_FLIGHT);
			descriptorSets = CreateDescriptorSets(context, descriptorPool, descriptorSetLayout, uniformBuffers.Span, textureImageView, sampler);

			PrintValidationLayers(context);
			PrintQueueFamilies(context);
			//PrintExtensions(context);
			PrintSurfaceCapabilities(context, surface);
			PrintMemoryTypes(context);
		}

		private void Window_Resize(Vector2D<int> obj)
		{
			framebufferResized = true;
		}

		void recreateSwapChain()
		{
			Vector2D<int> framebufferSize = window!.FramebufferSize;

			while (framebufferSize.X == 0 || framebufferSize.Y == 0)
			{
				framebufferSize = window.FramebufferSize;
				window.DoEvents();
			}

			context.vk.DeviceWaitIdle(context.device);
			DisposeSwapChain();

			Format depthFormat = FindSupportedFormat(context, [Format.D32Sfloat, Format.D32SfloatS8Uint, Format.D24UnormS8Uint], ImageTiling.Optimal, FormatFeatureFlags.DepthStencilAttachmentBit);

			var graphicsQueueFamily = GetGraphicsQueueFamily(context);
			swapchain = CreateSwapChain(context, surface, graphicsQueueFamily);

			var extent = ChooseSwapExtent(context, surface);
            images = GetSwapChainImages(context, swapchain);
			imageViews = CreateImageViews(context, images.Span, surface, ImageAspectFlags.ColorBit);

			depthImage = CreateTextureImage(context, extent, depthFormat, ImageTiling.Optimal, ImageUsageFlags.DepthStencilAttachmentBit);
			depthImageMemory = CreateImageMemory(context, depthImage, MemoryPropertyFlags.DeviceLocalBit);
			depthImageView = CreateImageView(context, depthImage, depthFormat, ImageAspectFlags.DepthBit);

			renderPass = CreateRenderPass(context, surface, depthFormat);
			frameBuffers = CreateFrameBuffers(context, imageViews.Span, depthImageView, renderPass, extent);
			pipelineLayout = CreatePipelineLayout(context, descriptorSetLayout);

			pipeline = CreateGraphicsPipeline(context, extent, pipelineLayout, renderPass);
			commandBuffers = CreateCommandBuffers(context, commandPool, MAX_FRAMES_IN_FLIGHT);
		}

		public unsafe void Dispose()
		{
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
		}

		public unsafe void PreRun()
		{
            window.DoEvents();

            Thread.Sleep(10);

            Span<Fence> fences = [inFlightFences.Span[currentFrame]];
			context.vk.WaitForFences(context.device, 1, fences, true, ulong.MaxValue);

			context.vk.TryGetDeviceExtension(context.instance, context.device, out KhrSwapchain khrSwapChain);

			uint imageIndex = 0;
			var aquireResult = khrSwapChain.AcquireNextImage(context.device, swapchain, ulong.MaxValue, imageAvailableSemaphores.Span[currentFrame], default, ref imageIndex);
			if (aquireResult == Result.ErrorOutOfDateKhr || framebufferResized)
			{
				framebufferResized = false;
				recreateSwapChain();
				return;
			}

			context.vk.WaitForFences(context.device, 1, fences, true, ulong.MaxValue);

			context.vk.ResetCommandBuffer(commandBuffers.Span[currentFrame], 0);

			var extent = ChooseSwapExtent(context, surface);

			UniformBufferUpdate(ref uniformBufferObjects.Span[currentFrame].Value, extent);
			RecordCommandBuffer(context, commandBuffers.Span[currentFrame], renderPass, frameBuffers.Span[(int)imageIndex], new Rect2D(new(), extent), pipeline, vertexBuffer, indexBuffer, indiciesLength, pipelineLayout, descriptorSets.Span[currentFrame]);

			SubmitInfo submitInfo = new();
			submitInfo.SType = StructureType.SubmitInfo;

			Span<Silk.NET.Vulkan.Semaphore> semaphores = [imageAvailableSemaphores.Span[currentFrame]];
			Span<PipelineStageFlags> waitStages = [PipelineStageFlags.ColorAttachmentOutputBit];

			submitInfo.WaitSemaphoreCount = 1;
			fixed (Silk.NET.Vulkan.Semaphore* semaphoresPtr = semaphores)
			fixed (PipelineStageFlags* waitStagesPtr = waitStages)
			{
				submitInfo.PWaitSemaphores = semaphoresPtr;
				submitInfo.PWaitDstStageMask = waitStagesPtr;
			}

			submitInfo.CommandBufferCount = 1;

			fixed (CommandBuffer* commandBufferPtr = &commandBuffers.Span[currentFrame])
			{
				submitInfo.PCommandBuffers = commandBufferPtr;
			}

			Span<Silk.NET.Vulkan.Semaphore> signalSemaphores = [imageAvailableSemaphores.Span[currentFrame]];

			submitInfo.SignalSemaphoreCount = 1;
			fixed (Silk.NET.Vulkan.Semaphore* signalSemaphoresPtr = signalSemaphores)
			{
				submitInfo.PSignalSemaphores = signalSemaphoresPtr;
			}

			context.vk.ResetFences(context.device, fences);
			var result = context.vk.QueueSubmit(graphicsQueue, 1, submitInfo, inFlightFences.Span[currentFrame]);
            if (result != Result.Success)
				throw new Exception("Failed to submit draw command buffer!");

            PresentInfoKHR presentInfo = new();
			presentInfo.SType = StructureType.PresentInfoKhr;
			presentInfo.WaitSemaphoreCount = 1;

			fixed (Silk.NET.Vulkan.Semaphore* signalSemaphoresPtr = signalSemaphores)
			{
				presentInfo.PWaitSemaphores = signalSemaphoresPtr;
			}

			Span<SwapchainKHR> swapchains = [swapchain];

			presentInfo.SwapchainCount = 1;

			fixed (SwapchainKHR* swapchainPtr = swapchains)
			{
				presentInfo.PSwapchains = swapchainPtr;
			}

			presentInfo.PImageIndices = &imageIndex;
			presentInfo.PResults = null;

			var presentResult = khrSwapChain.QueuePresent(presentQueue, presentInfo);
			if (presentResult == Result.ErrorOutOfDateKhr || presentResult == Result.SuboptimalKhr)
			{
				recreateSwapChain();
			}

			currentFrame = (currentFrame + 1) % MAX_FRAMES_IN_FLIGHT;
		}

		public unsafe void Update(Position.Ref position)
		{
            
		}

		public void PostRun()
		{

		}

		unsafe void DisposeSwapChain()
		{
			foreach (var frameBuffer in frameBuffers.Span)
			{
				context.vk.DestroyFramebuffer(context.device, frameBuffer, null);
			}

			foreach (var imageView in imageViews.Span)
			{
				context.vk.DestroyImageView(context.device, imageView, null);
			}

			fixed (CommandBuffer* commandBuffersPtr = commandBuffers.Span)
			{
				context.vk.FreeCommandBuffers(context.device, commandPool, (uint)commandBuffers.Length, commandBuffersPtr);
			}

			context.vk.DestroyPipeline(context.device, pipeline, null);
			context.vk.DestroyPipelineLayout(context.device, pipelineLayout, null);
			context.vk.DestroyRenderPass(context.device, renderPass, null);

			context.vk.TryGetDeviceExtension(context.instance, context.device, out KhrSwapchain khrSwapChain);
			khrSwapChain.DestroySwapchain(context.device, swapchain, null);
		}

		static float time = 0;

		static void UniformBufferUpdate(ref UniformBufferObject ubo, Extent2D size)
		{
			Matrix4x4 translationMatrix = Matrix4x4.CreateTranslation(new Vector3(0, 0, 0));
			Matrix4x4 rotationMatrix = Matrix4x4.CreateFromAxisAngle(new Vector3(0, 0, 1), 1.57f * time);
			Matrix4x4 scaleMatrix = Matrix4x4.CreateScale(new Vector3(1, 1, 1));

			time += 0.01f;

			ubo = new UniformBufferObject()
			{
				translation = translationMatrix,
				rotation = rotationMatrix,
				scale = scaleMatrix,
				view = Matrix4x4.CreateLookAt(new Vector3(2, 2, 2), new Vector3(0, 0, 0), new Vector3(0, 0, 1)),
				proj = Matrix4x4.CreatePerspectiveFieldOfView(0.6981f, (float)size.Width / size.Height, 0.1f, 10f)
			};

			ubo.proj.M22 *= -1;
		}

		static unsafe uint GetGraphicsQueueFamily(VkContext context)
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

		static unsafe uint GetPresentQueueFamily(VkContext context, SurfaceKHR surface)
		{
			context.vk.TryGetInstanceExtension(context.instance, out KhrSurface khrSurface);

			Span<QueueFamilyProperties> qFamilies = stackalloc QueueFamilyProperties[16];
			qFamilies = GetQueueFamilies(context, qFamilies, context.physicalDevice);

			for (uint i = 0; i < qFamilies.Length; i++)
			{
				khrSurface.GetPhysicalDeviceSurfaceSupport(context.physicalDevice, i, surface, out Bool32 supported);

				if (supported.Value == 1)
					return i;
			}

			throw new Exception("Queue with graphics bit not found.");
		}

		static unsafe Queue GetQueue(VkContext context, uint familyIndex)
		{
			context.vk.GetDeviceQueue(context.device, familyIndex, 0, out Queue queue);

			return queue;
		}

		static unsafe SurfaceKHR CreateSurface(VkContext context)
		{
			return context.window.VkSurface.Create<AllocationCallbacks>(context.instance.ToHandle(), null).ToSurface();
		}

		static unsafe SwapchainKHR CreateSwapChain(VkContext context, SurfaceKHR surface, uint graphicsQueueFamily)
		{
			context.vk.TryGetDeviceExtension(context.instance, context.device, out KhrSwapchain khrSwapChain);

            var format = ChooseSwapSurfaceFormat(context, surface);
			var presentMode = ChooseSwapPresentMode(context, surface);
			var extent = ChooseSwapExtent(context, surface);
			var imageCount = ChooseImageCount(context, surface);
			var transform = ChooseSwapSurfaceTransform(context, surface);

			var presentQueueFamily = GetPresentQueueFamily(context, surface);

			SwapchainCreateInfoKHR createInfo = new();
			createInfo.SType = StructureType.SwapchainCreateInfoKhr;
			createInfo.Surface = surface;

			createInfo.MinImageCount = imageCount;
			createInfo.ImageFormat = format.Format;
			createInfo.ImageColorSpace = format.ColorSpace;
			createInfo.ImageExtent = extent;
			createInfo.ImageArrayLayers = 1;
			createInfo.ImageUsage = ImageUsageFlags.ColorAttachmentBit;

			if (graphicsQueueFamily != presentQueueFamily)
			{
				var familyIndicies = stackalloc uint[] { graphicsQueueFamily, presentQueueFamily };

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

		static unsafe ImageView CreateImageView(VkContext context, Silk.NET.Vulkan.Image image, Format format, ImageAspectFlags aspectMask)
		{
			ImageViewCreateInfo createInfo = new();
			createInfo.SType = StructureType.ImageViewCreateInfo;
			createInfo.Image = image;

			createInfo.ViewType = ImageViewType.ImageViewType2D;
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

		static unsafe Memory<ImageView> CreateImageViews(VkContext context, Span<Silk.NET.Vulkan.Image> images, SurfaceKHR surface, ImageAspectFlags aspectMask)
		{
			var surfaceFormat = ChooseSwapSurfaceFormat(context, surface);

			Memory<ImageView> imageViews = new ImageView[images.Length];
			for (int i = 0; i < imageViews.Length; i++)
			{
				imageViews.Span[i] = CreateImageView(context, images[i], surfaceFormat.Format, aspectMask);
			}

			return imageViews;
		}

		static unsafe Memory<Framebuffer> CreateFrameBuffers(VkContext context, Span<ImageView> imageViews, ImageView depthImageView, RenderPass renderPass, Extent2D swapchainExtent)
		{
			Memory<Framebuffer> frameBuffers = new Framebuffer[imageViews.Length];
			for (int i = 0; i < frameBuffers.Length; i++)
			{
				FramebufferCreateInfo createInfo = new();
				createInfo.SType = StructureType.FramebufferCreateInfo;
				createInfo.RenderPass = renderPass;
				createInfo.AttachmentCount = 2;

				ImageView* attachments = stackalloc ImageView[2];
				attachments[0] = imageViews[i];
				attachments[1] = depthImageView;

				createInfo.PAttachments = attachments;
				createInfo.Width = swapchainExtent.Width;
				createInfo.Height = swapchainExtent.Height;
				createInfo.Layers = 1;

				var result = context.vk.CreateFramebuffer(context.device, createInfo, null, out Framebuffer framebuffer);
				if (result != Result.Success)
					throw new Exception("Failed to create vkImageView");

				frameBuffers.Span[i] = framebuffer;
			}

			return frameBuffers;
		}

		static unsafe Pipeline CreateGraphicsPipeline(VkContext context, Extent2D swapchainExtent, PipelineLayout pipelineLayout, RenderPass renderPass)
		{
			var shader = Shader.FromFiles("Shaders/VulkanVert.spv", "Shaders/VulkanFrag.spv");

			var vertShader = CreateShaderModule(context.vk, shader.Vertex, context.device);
			var fragShader = CreateShaderModule(context.vk, shader.Fragment, context.device);

			PipelineShaderStageCreateInfo vertexStageCreateInfo = new();
			vertexStageCreateInfo.SType = StructureType.PipelineShaderStageCreateInfo;
			vertexStageCreateInfo.Stage = ShaderStageFlags.VertexBit;

			vertexStageCreateInfo.Module = vertShader;
			vertexStageCreateInfo.PName = (byte*)Marshal.StringToHGlobalAnsi("main");

			PipelineShaderStageCreateInfo fragmentStageCreateInfo = new();
			fragmentStageCreateInfo.SType = StructureType.PipelineShaderStageCreateInfo;
			fragmentStageCreateInfo.Stage = ShaderStageFlags.FragmentBit;

			fragmentStageCreateInfo.Module = fragShader;
			fragmentStageCreateInfo.PName = (byte*)Marshal.StringToHGlobalAnsi("main");

			Span<PipelineShaderStageCreateInfo> shaderStages = [vertexStageCreateInfo, fragmentStageCreateInfo];

			DynamicState* dynamicState = stackalloc DynamicState[2] { DynamicState.Viewport, DynamicState.Scissor };

			PipelineDynamicStateCreateInfo dynamicStateCreateInfo = new();
			dynamicStateCreateInfo.SType = StructureType.PipelineDynamicStateCreateInfo;
			dynamicStateCreateInfo.DynamicStateCount = 2;
			dynamicStateCreateInfo.PDynamicStates = &dynamicState[0];

			VertexInputBindingDescription bindingDescription = GetBindingDescription();
			Memory<VertexInputAttributeDescription> attributeDescription = GetAttributeDescription();

			PipelineVertexInputStateCreateInfo vertexInputStateCreateInfo = new();
			vertexInputStateCreateInfo.SType = StructureType.PipelineVertexInputStateCreateInfo;
			vertexInputStateCreateInfo.VertexBindingDescriptionCount = 1;
			vertexInputStateCreateInfo.PVertexBindingDescriptions = &bindingDescription;
			vertexInputStateCreateInfo.VertexAttributeDescriptionCount = (uint)attributeDescription.Length;
			fixed(VertexInputAttributeDescription* attributeDescriptionPtr = attributeDescription.Span)
			{
				vertexInputStateCreateInfo.PVertexAttributeDescriptions = attributeDescriptionPtr;
			}

			PipelineInputAssemblyStateCreateInfo inputAssemblyStateCreateInfo = new();
			inputAssemblyStateCreateInfo.SType = StructureType.PipelineInputAssemblyStateCreateInfo;
			inputAssemblyStateCreateInfo.Topology = PrimitiveTopology.TriangleList;
			inputAssemblyStateCreateInfo.PrimitiveRestartEnable = false;

			Viewport viewport = new();
			viewport.X = 0;
			viewport.Y = 0;
			viewport.Width = swapchainExtent.Width;
			viewport.Height = swapchainExtent.Height;
			viewport.MinDepth = 0;
			viewport.MaxDepth = 1;

			Rect2D scissor = new();
			scissor.Offset = new(0, 0);
			scissor.Extent = swapchainExtent;

			PipelineViewportStateCreateInfo viewportStateCreateInfo = new();
			viewportStateCreateInfo.SType = StructureType.PipelineViewportStateCreateInfo;
			viewportStateCreateInfo.ViewportCount = 1;
			viewportStateCreateInfo.PViewports = &viewport;
			viewportStateCreateInfo.ScissorCount = 1;
			viewportStateCreateInfo.PScissors = &scissor;

			PipelineRasterizationStateCreateInfo rasterizationStateCreateInfo = new();
			rasterizationStateCreateInfo.SType = StructureType.PipelineRasterizationStateCreateInfo;
			rasterizationStateCreateInfo.DepthClampEnable = false;
			rasterizationStateCreateInfo.RasterizerDiscardEnable = false;
			rasterizationStateCreateInfo.PolygonMode = PolygonMode.Fill;
			rasterizationStateCreateInfo.LineWidth = 1.0f;
			rasterizationStateCreateInfo.CullMode = CullModeFlags.BackBit;
			rasterizationStateCreateInfo.FrontFace = FrontFace.CounterClockwise;
			rasterizationStateCreateInfo.DepthBiasEnable = false;
			rasterizationStateCreateInfo.DepthBiasConstantFactor = 0;
			rasterizationStateCreateInfo.DepthBiasClamp = 0;
			rasterizationStateCreateInfo.DepthBiasSlopeFactor = 0;

			PipelineMultisampleStateCreateInfo multisampleStateCreateInfo = new();
			multisampleStateCreateInfo.SType = StructureType.PipelineMultisampleStateCreateInfo;
			multisampleStateCreateInfo.SampleShadingEnable = false;
			multisampleStateCreateInfo.RasterizationSamples = SampleCountFlags.Count1Bit;
			multisampleStateCreateInfo.MinSampleShading = 1;
			multisampleStateCreateInfo.PSampleMask = null;
			multisampleStateCreateInfo.AlphaToCoverageEnable = false;
			multisampleStateCreateInfo.AlphaToOneEnable = false;

			PipelineColorBlendAttachmentState colorBlendAttatchment = new();
			colorBlendAttatchment.ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit;
			colorBlendAttatchment.BlendEnable = false;
			colorBlendAttatchment.SrcColorBlendFactor = BlendFactor.One;
			colorBlendAttatchment.DstColorBlendFactor = BlendFactor.Zero;
			colorBlendAttatchment.ColorBlendOp = BlendOp.Add;
			colorBlendAttatchment.SrcAlphaBlendFactor = BlendFactor.One;
			colorBlendAttatchment.DstAlphaBlendFactor = BlendFactor.One;
			colorBlendAttatchment.AlphaBlendOp = BlendOp.Add;

			PipelineColorBlendStateCreateInfo colorBlendStateCreateInfo = new();
			colorBlendStateCreateInfo.SType = StructureType.PipelineColorBlendStateCreateInfo;
			colorBlendStateCreateInfo.LogicOpEnable = false;
			colorBlendStateCreateInfo.LogicOp = LogicOp.Copy;
			colorBlendStateCreateInfo.AttachmentCount = 1;
			colorBlendStateCreateInfo.PAttachments = &colorBlendAttatchment;
			colorBlendStateCreateInfo.BlendConstants[0] = 0;
			colorBlendStateCreateInfo.BlendConstants[1] = 0;
			colorBlendStateCreateInfo.BlendConstants[2] = 0;
			colorBlendStateCreateInfo.BlendConstants[3] = 0;

			PipelineDepthStencilStateCreateInfo depthStencilStateCreateInfo = new();
			depthStencilStateCreateInfo.SType = StructureType.PipelineDepthStencilStateCreateInfo;
			depthStencilStateCreateInfo.DepthTestEnable = true;
			depthStencilStateCreateInfo.DepthWriteEnable = true;
			depthStencilStateCreateInfo.DepthCompareOp = CompareOp.Less;
			depthStencilStateCreateInfo.DepthBoundsTestEnable = false;
			depthStencilStateCreateInfo.MinDepthBounds = 0.0f;
			depthStencilStateCreateInfo.MaxDepthBounds = 1.0f;
			depthStencilStateCreateInfo.StencilTestEnable = false;
			depthStencilStateCreateInfo.Front = default;
			depthStencilStateCreateInfo.Back = default;

			GraphicsPipelineCreateInfo graphicsCreateInfo = new();
			graphicsCreateInfo.SType = StructureType.GraphicsPipelineCreateInfo;
			graphicsCreateInfo.StageCount = (uint)shaderStages.Length;
			fixed (PipelineShaderStageCreateInfo* shaderStagesPtr = shaderStages)
			{
				graphicsCreateInfo.PStages = shaderStagesPtr;
			}

			graphicsCreateInfo.PVertexInputState = &vertexInputStateCreateInfo;
			graphicsCreateInfo.PInputAssemblyState = &inputAssemblyStateCreateInfo;
			graphicsCreateInfo.PViewportState = &viewportStateCreateInfo;
			graphicsCreateInfo.PRasterizationState = &rasterizationStateCreateInfo;
			graphicsCreateInfo.PMultisampleState = &multisampleStateCreateInfo;
			graphicsCreateInfo.PDepthStencilState = null;
			graphicsCreateInfo.PColorBlendState = &colorBlendStateCreateInfo;
			graphicsCreateInfo.PDynamicState = &dynamicStateCreateInfo;
			graphicsCreateInfo.Layout = pipelineLayout;
			graphicsCreateInfo.RenderPass = renderPass;
			graphicsCreateInfo.Subpass = 0;
			graphicsCreateInfo.BasePipelineHandle = default;
			graphicsCreateInfo.BasePipelineIndex = -1;
			graphicsCreateInfo.PDepthStencilState = &depthStencilStateCreateInfo;

			var result = context.vk.CreateGraphicsPipelines(context.device, default, 1, graphicsCreateInfo, null, out Pipeline pipeline);
			if (result != Result.Success)
				throw new Exception("Failed to create vkPipelineLayout");

			context.vk.DestroyShaderModule(context.device, vertShader, null);
			context.vk.DestroyShaderModule(context.device, fragShader, null);

			return pipeline;
		}

		static unsafe PipelineLayout CreatePipelineLayout(VkContext context, DescriptorSetLayout descriptorSetLayout)
		{
			PipelineColorBlendAttachmentState colorBlendAttatchment = new();
			colorBlendAttatchment.ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit;
			colorBlendAttatchment.BlendEnable = false;
			colorBlendAttatchment.SrcColorBlendFactor = BlendFactor.One;
			colorBlendAttatchment.DstColorBlendFactor = BlendFactor.Zero;
			colorBlendAttatchment.ColorBlendOp = BlendOp.Add;
			colorBlendAttatchment.SrcAlphaBlendFactor = BlendFactor.One;
			colorBlendAttatchment.DstAlphaBlendFactor = BlendFactor.One;
			colorBlendAttatchment.AlphaBlendOp = BlendOp.Add;

			PipelineLayoutCreateInfo pipelineLayoutCreateInfo = new();
			pipelineLayoutCreateInfo.SType = StructureType.PipelineLayoutCreateInfo;
			pipelineLayoutCreateInfo.SetLayoutCount = 1;
			pipelineLayoutCreateInfo.PSetLayouts = &descriptorSetLayout;
			pipelineLayoutCreateInfo.PushConstantRangeCount = 0;
			pipelineLayoutCreateInfo.PPushConstantRanges = null;

			var result = context.vk.CreatePipelineLayout(context.device, pipelineLayoutCreateInfo, null, out PipelineLayout pipelineLayout);
			if (result != Result.Success)
				throw new Exception("Failed to create vkPipelineLayout");

			return pipelineLayout;
		}

		static unsafe RenderPass CreateRenderPass(VkContext context, SurfaceKHR surface, Format depthFormat)
		{
			var surfaceFormat = ChooseSwapSurfaceFormat(context, surface);

			AttachmentDescription colorAttachment = new();
			colorAttachment.Format = surfaceFormat.Format;
			colorAttachment.Samples = SampleCountFlags.Count1Bit;
			colorAttachment.LoadOp = AttachmentLoadOp.Clear;
			colorAttachment.StoreOp = AttachmentStoreOp.Store;
			colorAttachment.StencilLoadOp = AttachmentLoadOp.DontCare;
			colorAttachment.StencilStoreOp = AttachmentStoreOp.DontCare;
			colorAttachment.InitialLayout = ImageLayout.Undefined;
			colorAttachment.FinalLayout = ImageLayout.PresentSrcKhr;

			AttachmentDescription depthAttatchment = new();
			depthAttatchment.Format = depthFormat;
			depthAttatchment.Samples = SampleCountFlags.Count1Bit;
			depthAttatchment.LoadOp = AttachmentLoadOp.Clear;
			depthAttatchment.StoreOp = AttachmentStoreOp.DontCare;
			depthAttatchment.StencilLoadOp = AttachmentLoadOp.DontCare;
			depthAttatchment.StencilStoreOp = AttachmentStoreOp.DontCare;
			depthAttatchment.InitialLayout = ImageLayout.Undefined;
			depthAttatchment.FinalLayout = ImageLayout.DepthStencilAttachmentOptimal;

			AttachmentReference colorAttachmentRef = new();
			colorAttachmentRef.Attachment = 0;
			colorAttachmentRef.Layout = ImageLayout.ColorAttachmentOptimal;

			AttachmentReference depthAttachmentRef = new();
			depthAttachmentRef.Attachment = 1;
			depthAttachmentRef.Layout = ImageLayout.DepthStencilAttachmentOptimal;

			SubpassDescription subpass = new();
			subpass.PipelineBindPoint = PipelineBindPoint.Graphics;
			subpass.ColorAttachmentCount = 1;
			subpass.PColorAttachments = &colorAttachmentRef;
			subpass.PDepthStencilAttachment = &depthAttachmentRef;

			SubpassDependency dependency = new();
			dependency.SrcSubpass = Vk.SubpassExternal;
			dependency.DstSubpass = 0;
			dependency.SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit;
			dependency.SrcAccessMask = 0;
			dependency.DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit;
			dependency.DstAccessMask = AccessFlags.ColorAttachmentWriteBit | AccessFlags.DepthStencilAttachmentWriteBit;

			AttachmentDescription* attachments = stackalloc AttachmentDescription[2];
			attachments[0] = colorAttachment;
			attachments[1] = depthAttatchment;

			RenderPassCreateInfo createInfo = new();
			createInfo.SType = StructureType.RenderPassCreateInfo;
			createInfo.AttachmentCount = 2;
			createInfo.PAttachments = attachments;
			createInfo.SubpassCount = 1;
			createInfo.PSubpasses = &subpass;
			createInfo.DependencyCount = 1;
			createInfo.PDependencies = &dependency;

			var result = context.vk.CreateRenderPass(context.device, createInfo, null, out RenderPass renderPass);
			if (result != Result.Success)
				throw new Exception("Failed to create vkRenderPass");

			return renderPass;
		}

		static unsafe ShaderModule CreateShaderModule(Vk vk, byte[] code, Device device)
		{
			ShaderModuleCreateInfo createInfo = new();
			createInfo.SType = StructureType.ShaderModuleCreateInfo;
			createInfo.CodeSize = (nuint)code.Length;

			fixed(byte* codePtr = code)
			{
				createInfo.PCode = (uint*)codePtr;
			}

			var result = vk.CreateShaderModule(device, createInfo, null, out ShaderModule shaderModule);
			if (result != Result.Success)
				throw new Exception("Failed to create vkShaderModule");

			return shaderModule;
		}

		static unsafe CommandPool CreateCommandPool(VkContext context, uint graphicsQueueFamily)
		{
			CommandPoolCreateInfo createInfo = new();
			createInfo.SType = StructureType.CommandPoolCreateInfo;
			createInfo.Flags = CommandPoolCreateFlags.ResetCommandBufferBit;
			createInfo.QueueFamilyIndex = graphicsQueueFamily;

			var result = context.vk.CreateCommandPool(context.device, createInfo, null, out CommandPool commandPool);
			if (result != Result.Success)
				throw new Exception("Failed to create vkShaderModule");

			return commandPool;
		}

		static unsafe Memory<CommandBuffer> CreateCommandBuffers(VkContext context, CommandPool commandPool, uint count)
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
					throw new Exception("Failed to create vkShaderModule");
			}

			return commandBuffers;
		}

		static unsafe CommandBuffer CreateCommandBuffer(VkContext context, CommandPool commandPool)
		{
			CommandBufferAllocateInfo allocInfo = new();
			allocInfo.SType = StructureType.CommandBufferAllocateInfo;
			allocInfo.CommandPool = commandPool;
			allocInfo.Level = CommandBufferLevel.Primary;
			allocInfo.CommandBufferCount = 1;

			var result = context.vk.AllocateCommandBuffers(context.device, allocInfo, out CommandBuffer commandBuffer);
			if (result != Result.Success)
				throw new Exception("Failed to create vkShaderModule");

			return commandBuffer;
		}

		static unsafe Memory<Silk.NET.Vulkan.Semaphore> CreateSemaphores(VkContext context)
		{
			Memory<Silk.NET.Vulkan.Semaphore> semaphores = new Silk.NET.Vulkan.Semaphore[MAX_FRAMES_IN_FLIGHT];

			SemaphoreCreateInfo createInfo = new();
			createInfo.SType = StructureType.SemaphoreCreateInfo;

			for (int i = 0; i < semaphores.Length; i++)
			{
				var result = context.vk.CreateSemaphore(context.device, createInfo, null, out semaphores.Span[i]);
				if (result != Result.Success)
					throw new Exception("Failed to create vkShaderModule");
			}

			return semaphores;
		}

		static unsafe Memory<Fence> CreateFences(VkContext context, FenceCreateFlags flags)
		{
			Memory<Fence> fences = new Fence[MAX_FRAMES_IN_FLIGHT];

			FenceCreateInfo createInfo = new();
			createInfo.SType = StructureType.FenceCreateInfo;
			createInfo.Flags = flags;

			for (int i = 0; i < fences.Length; i++)
			{
				var result = context.vk.CreateFence(context.device, createInfo, null, out fences.Span[i]);
				if (result != Result.Success)
					throw new Exception("Failed to create vkShaderModule");
			}

			return fences;
		}

		static unsafe Silk.NET.Vulkan.Buffer CreateBuffer<T>(VkContext context, BufferUsageFlags bufferUsage, uint dataCount) where T : unmanaged
		{
			BufferCreateInfo createInfo = new();
			createInfo.SType = StructureType.BufferCreateInfo;
			createInfo.Size = (uint)sizeof(T) * dataCount;
			createInfo.Usage = bufferUsage;
			createInfo.SharingMode = SharingMode.Exclusive;
			createInfo.Flags = BufferCreateFlags.None;

			var result = context.vk.CreateBuffer(context.device, createInfo, null, out Silk.NET.Vulkan.Buffer buffer);
			if (result != Result.Success)
				throw new Exception("Failed to create vkBuffer");

			return buffer;
		}

		static Memory<Silk.NET.Vulkan.Buffer> CreateBuffers<T>(VkContext context, BufferUsageFlags bufferUsage, uint dataCount, uint bufferCount) where T : unmanaged
		{
			Memory<Silk.NET.Vulkan.Buffer> buffers = new Silk.NET.Vulkan.Buffer[bufferCount];
			for (int i = 0; i < buffers.Length; i++)
			{
				buffers.Span[i] = CreateBuffer<T>(context, bufferUsage, dataCount);
			}

			return buffers;
		}

		static unsafe DeviceMemory CreateBufferMemory(VkContext context, Silk.NET.Vulkan.Buffer buffer, MemoryPropertyFlags properties)
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

		static unsafe DeviceMemory CreateImageMemory(VkContext context, Silk.NET.Vulkan.Image image, MemoryPropertyFlags properties)
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

		static Memory<DeviceMemory> CreateBufferMemories(VkContext context, Span<Silk.NET.Vulkan.Buffer> buffers, MemoryPropertyFlags properties)
		{
			Memory<DeviceMemory> bufferMemories = new DeviceMemory[buffers.Length];
			for (int i = 0; i < buffers.Length; i++)
			{
				bufferMemories.Span[i] = CreateBufferMemory(context, buffers[i], properties);
			}

			return bufferMemories;
		}

		static unsafe void MapBufferMemory<T>(VkContext context, Silk.NET.Vulkan.Buffer buffer, DeviceMemory bufferMemory, T data) where T : unmanaged
		{
			context.vk.GetBufferMemoryRequirements(context.device, buffer, out MemoryRequirements memRequirements);

			void* dataPtr;
			context.vk.MapMemory(context.device, bufferMemory, 0, memRequirements.Size, 0, &dataPtr);
			((T*)dataPtr)[0] = data;
			context.vk.UnmapMemory(context.device, bufferMemory);
		}

		static unsafe void MapBufferMemory<T>(VkContext context, Silk.NET.Vulkan.Buffer buffer, DeviceMemory bufferMemory, Span<T> data) where T : unmanaged
		{
			context.vk.GetBufferMemoryRequirements(context.device, buffer, out MemoryRequirements memRequirements);

			void* dataPtr;
			context.vk.MapMemory(context.device, bufferMemory, 0, memRequirements.Size, 0, &dataPtr);
			data.CopyTo(new Span<T>(dataPtr, data.Length));
			context.vk.UnmapMemory(context.device, bufferMemory);
		}

		static unsafe DescriptorSetLayout CreateDescriptorSetLayout(VkContext context)
		{
			DescriptorSetLayoutBinding uniformBinding = new();
			uniformBinding.Binding = 0;
			uniformBinding.DescriptorType = DescriptorType.UniformBuffer;
			uniformBinding.DescriptorCount = 1;
			uniformBinding.StageFlags = ShaderStageFlags.VertexBit;
			uniformBinding.PImmutableSamplers = null;

			DescriptorSetLayoutBinding samplerBinding = new();
			samplerBinding.Binding = 1;
			samplerBinding.DescriptorType = DescriptorType.CombinedImageSampler;
			samplerBinding.DescriptorCount = 1;
			samplerBinding.StageFlags = ShaderStageFlags.FragmentBit;
			samplerBinding.PImmutableSamplers = null;

			DescriptorSetLayoutCreateInfo createInfo = new();
			createInfo.SType = StructureType.DescriptorSetLayoutCreateInfo;
			createInfo.BindingCount = 2;

			DescriptorSetLayoutBinding* bindingsPtr = stackalloc DescriptorSetLayoutBinding[2];
			bindingsPtr[0] = uniformBinding;
			bindingsPtr[1] = samplerBinding;

			createInfo.PBindings = bindingsPtr;

			var result = context.vk.CreateDescriptorSetLayout(context.device, createInfo, null, out DescriptorSetLayout descriptorSetLayout);
			if (result != Result.Success)
				throw new Exception("Failed to create vkDescriptorSetLayout");

			return descriptorSetLayout;
		}

		static unsafe DescriptorPool CreateDescriptorPool(VkContext context, uint size)
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

		static unsafe Memory<DescriptorSet> CreateDescriptorSets(VkContext context, DescriptorPool descriptorPool, DescriptorSetLayout layout, Span<Silk.NET.Vulkan.Buffer> uniformBuffers, ImageView textureImageView, Sampler sampler)
		{
			DescriptorSetLayout* layouts = stackalloc DescriptorSetLayout[MAX_FRAMES_IN_FLIGHT];
			for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
			{
				layouts[i] = layout;
			}

			DescriptorSetAllocateInfo allocInfo = new();
			allocInfo.SType = StructureType.DescriptorSetAllocateInfo;
			allocInfo.DescriptorPool = descriptorPool;
			allocInfo.DescriptorSetCount = MAX_FRAMES_IN_FLIGHT;
			allocInfo.PSetLayouts = layouts;

			Memory<DescriptorSet> descriptorSets = new DescriptorSet[MAX_FRAMES_IN_FLIGHT];
			fixed(DescriptorSet* escriptorSetsPtr = descriptorSets.Span)
			{
				var result = context.vk.AllocateDescriptorSets(context.device, allocInfo, escriptorSetsPtr);
				if (result != Result.Success)
					throw new Exception("Failed to allocate vkDescriptorSets");
			}

			for (int i = 0; i < descriptorSets.Length; i++)
			{
				DescriptorBufferInfo bufferInfo = new();
				bufferInfo.Buffer = uniformBuffers[i];
				bufferInfo.Offset = 0;
				bufferInfo.Range = (ulong)sizeof(UniformBufferObject);

				DescriptorImageInfo imageInfo = new();
				imageInfo.ImageLayout = ImageLayout.ReadOnlyOptimal;
				imageInfo.ImageView = textureImageView;
				imageInfo.Sampler = sampler;

				WriteDescriptorSet bufferDescriptorWrite = new();
				bufferDescriptorWrite.SType = StructureType.WriteDescriptorSet;
				bufferDescriptorWrite.DstSet = descriptorSets.Span[i];
				bufferDescriptorWrite.DstBinding = 0;
				bufferDescriptorWrite.DstArrayElement = 0;
				bufferDescriptorWrite.DescriptorType = DescriptorType.UniformBuffer;
				bufferDescriptorWrite.DescriptorCount = 1;
				bufferDescriptorWrite.PBufferInfo = &bufferInfo;

				WriteDescriptorSet imageDescriptorWrite = new();
				imageDescriptorWrite.SType = StructureType.WriteDescriptorSet;
				imageDescriptorWrite.DstSet = descriptorSets.Span[i];
				imageDescriptorWrite.DstBinding = 1;
				imageDescriptorWrite.DstArrayElement = 0;
				imageDescriptorWrite.DescriptorType = DescriptorType.CombinedImageSampler;
				imageDescriptorWrite.DescriptorCount = 1;
				imageDescriptorWrite.PImageInfo = &imageInfo;

				context.vk.UpdateDescriptorSets(context.device, [bufferDescriptorWrite, imageDescriptorWrite], 0, null);
			}

			return descriptorSets;
		}
		
		static unsafe Silk.NET.Vulkan.Image CreateTextureImage(VkContext context, Extent2D extent, Format format, ImageTiling tiling, ImageUsageFlags usage)
		{
			ImageCreateInfo createInfo = new();
			createInfo.SType = StructureType.ImageCreateInfo;
			createInfo.ImageType = ImageType.ImageType2D;
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

			var result = context.vk.CreateImage(context.device, createInfo, null, out Silk.NET.Vulkan.Image image);
			if (result != Result.Success)
				throw new Exception("Failed to create vkImage");

			return image;
		}

		static unsafe Sampler CreateTextureSampler(VkContext context)
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
				throw new Exception("Failed to create vkImage");

			return sampler;
		}

		static unsafe SurfaceFormatKHR ChooseSwapSurfaceFormat(VkContext context, SurfaceKHR surface)
		{
			context.vk.TryGetInstanceExtension(context.instance, out KhrSurface khrSurface);

			Span<SurfaceFormatKHR> surfaceFormats = stackalloc SurfaceFormatKHR[16];
			surfaceFormats = GetSurfaceFormats(surfaceFormats, khrSurface, context.physicalDevice, surface);

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
			presentModees = GetSurfacePresentModes(presentModees, khrSurface, context.physicalDevice, surface);

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

		static unsafe void RecordCommandBuffer(VkContext context, CommandBuffer commandBuffer, RenderPass renderPass, Framebuffer framebuffer, Rect2D renderArea, Pipeline graphicsPipeline, Silk.NET.Vulkan.Buffer vertexBuffers, Silk.NET.Vulkan.Buffer indexBuffer, uint indicies, PipelineLayout layout, DescriptorSet descriptorSet)
		{
			CommandBufferBeginInfo beginInfo = new();
			beginInfo.SType = StructureType.CommandBufferBeginInfo;
			beginInfo.Flags = 0;
			beginInfo.PInheritanceInfo = null;

			context.vk.BeginCommandBuffer(commandBuffer, beginInfo);

            RenderPassBeginInfo renderPassInfo = new();
			renderPassInfo.SType = StructureType.RenderPassBeginInfo;
			renderPassInfo.RenderPass = renderPass;
			renderPassInfo.Framebuffer = framebuffer;
			renderPassInfo.RenderArea = renderArea;

			System.Drawing.Color color = System.Drawing.Color.CornflowerBlue;

			ClearValue* clearColors = stackalloc ClearValue[2];
			clearColors[0] = new(color: new() { Float32_0 = color.R / 255f, Float32_1 = color.G / 255f, Float32_2 = color.B / 255f, Float32_3 = color.A / 255f });
			clearColors[1] = new(depthStencil: new(1.0f, 0));

			renderPassInfo.ClearValueCount = 2;
			renderPassInfo.PClearValues = clearColors;

			context.vk.CmdBeginRenderPass(commandBuffer, renderPassInfo, SubpassContents.Inline);
			context.vk.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, graphicsPipeline);

			Viewport viewport = new();
			viewport.X = 0;
			viewport.Y = 0;
			viewport.Width = renderArea.Extent.Width;
			viewport.Height = renderArea.Extent.Height;
			viewport.MinDepth = 0f;
			viewport.MaxDepth = 1f;

			context.vk.CmdSetViewport(commandBuffer, 0, 1, viewport);

			Rect2D scissor = new();
			scissor.Offset = new(0, 0);
			scissor.Extent = renderArea.Extent;

			context.vk.CmdSetScissor(commandBuffer, 0, 1, &scissor);

			context.vk.CmdBindVertexBuffers(commandBuffer, 0, [vertexBuffers], [0]);
			context.vk.CmdBindIndexBuffer(commandBuffer, indexBuffer, 0, IndexType.Uint16);

			context.vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, layout, 0, 1, descriptorSet, 0, null);

			context.vk.CmdDrawIndexed(commandBuffer, indicies, 1, 0, 0, 0);
			context.vk.CmdEndRenderPass(commandBuffer);

			var result = context.vk.EndCommandBuffer(commandBuffer);
			if (result != Result.Success)
				throw new Exception("Failed to end vkCommandBuffer");
		}

		static unsafe void PrintExtensions(VkContext context)
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

		static unsafe void PrintSurfaceCapabilities(VkContext context, SurfaceKHR surface)
		{
			context.vk.TryGetInstanceExtension(context.instance, out KhrSurface khrSurface);

			khrSurface.GetPhysicalDeviceSurfaceCapabilities(context.physicalDevice, surface, out SurfaceCapabilitiesKHR capabilities);
            Console.WriteLine($"Extent: {capabilities.CurrentExtent.Width}, {capabilities.CurrentExtent.Height}");
            Console.WriteLine($"Image Count: {capabilities.MinImageCount}-{capabilities.MaxImageCount}");

            Span<SurfaceFormatKHR> surfaceFormats = stackalloc SurfaceFormatKHR[16];
			surfaceFormats = GetSurfaceFormats(surfaceFormats, khrSurface, context.physicalDevice, surface);

			Span<PresentModeKHR> presentModees = stackalloc PresentModeKHR[16];
			presentModees = GetSurfacePresentModes(presentModees, khrSurface, context.physicalDevice, surface);

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

		static unsafe void PrintQueueFamilies(VkContext context)
		{
			Span<QueueFamilyProperties> qFamilies = stackalloc QueueFamilyProperties[16];
			qFamilies = GetQueueFamilies(context, qFamilies, context.physicalDevice);

			Console.WriteLine("Queue families:");
			foreach (var family in qFamilies)
			{
				Console.WriteLine($"\tCount: {family.QueueCount}, Flags: {family.QueueFlags}");
			}
		}

		static unsafe void PrintValidationLayers(VkContext context)
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

		static void PrintMemoryTypes(VkContext context)
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

		static unsafe Span<QueueFamilyProperties> GetQueueFamilies(VkContext context, Span<QueueFamilyProperties> buff, PhysicalDevice physicalDevice)
		{
			uint qFamilyCount = 0;
			context.vk.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, ref qFamilyCount, null);

			fixed (QueueFamilyProperties* buffPtr = buff)
			{
				context.vk.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, ref qFamilyCount, buffPtr);
			}

			return buff.Slice(0, (int)qFamilyCount);
		}

		static unsafe Span<SurfaceFormatKHR> GetSurfaceFormats(Span<SurfaceFormatKHR> buff, KhrSurface khrSurface, PhysicalDevice physicalDevice, SurfaceKHR surface)
		{
			uint formatCount = 0;
			khrSurface.GetPhysicalDeviceSurfaceFormats(physicalDevice, surface, ref formatCount, null);
			khrSurface.GetPhysicalDeviceSurfaceFormats(physicalDevice, surface, &formatCount, buff);

			return buff.Slice(0, (int)formatCount);
		}

		static unsafe Span<PresentModeKHR> GetSurfacePresentModes(Span<PresentModeKHR> buff, KhrSurface khrSurface, PhysicalDevice physicalDevice, SurfaceKHR surface)
		{
			uint presentModeCount = 0;
			khrSurface.GetPhysicalDeviceSurfacePresentModes(physicalDevice, surface, ref presentModeCount, null);
			khrSurface.GetPhysicalDeviceSurfacePresentModes(physicalDevice, surface, &presentModeCount, buff);

			return buff.Slice(0, (int)presentModeCount);
		}

		static unsafe Memory<Silk.NET.Vulkan.Image> GetSwapChainImages(VkContext context, SwapchainKHR swapchain)
		{
			context.vk.TryGetDeviceExtension(context.instance, context.device, out KhrSwapchain khrSwapChain);

			uint imageCount = 0;
			khrSwapChain.GetSwapchainImages(context.device, swapchain, ref imageCount, null);

			Memory<Silk.NET.Vulkan.Image> images = new Silk.NET.Vulkan.Image[imageCount];
			khrSwapChain.GetSwapchainImages(context.device, swapchain, &imageCount, images.Span);

			return images;
		}

		static unsafe bool CheckValidationLayerSupport(Vk vk, string[] validationLayers)
		{
			uint layerCount = 0;
			vk.EnumerateInstanceLayerProperties(ref layerCount, null);

			Span<LayerProperties> layerProperties = stackalloc LayerProperties[(int)layerCount];
			vk.EnumerateInstanceLayerProperties(ref layerCount, ref layerProperties[0]);

			int match = 0;
			foreach (var layer in layerProperties)
			{
				var layerName = Encoding.UTF8.GetString(layer.LayerName, 256).Trim().Replace("\0", "");

                if (validationLayers.Contains(layerName))
					match++;
			}

			return match == validationLayers.Length;
		}

		static VertexInputBindingDescription GetBindingDescription()
		{
			VertexInputBindingDescription description = new ();
			description.Binding = 0;
			description.Stride = Vertex.SizeInBytes;
			description.InputRate = VertexInputRate.Vertex;

			return description;
		}

		static Memory<VertexInputAttributeDescription> GetAttributeDescription()
		{
			Memory<VertexInputAttributeDescription> description = new VertexInputAttributeDescription[3];
			description.Span[0].Binding = 0;
			description.Span[0].Location = 0;
			description.Span[0].Format = Format.R32G32B32Sfloat;
			description.Span[0].Offset = 0;

			description.Span[1].Binding = 0;
			description.Span[1].Location = 1;
			description.Span[1].Format = Format.R32G32B32Sfloat;
			description.Span[1].Offset = sizeof(float) * 3;

			description.Span[2].Binding = 0;
			description.Span[2].Location = 2;
			description.Span[2].Format = Format.R32G32Sfloat;
			description.Span[2].Offset = sizeof(float) * 3 * 2;

			return description;
		}

		static uint FindMemoryType(VkContext context, uint typeFilter, MemoryPropertyFlags properties)
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

		static Format FindSupportedFormat(VkContext context, Span<Format> formats, ImageTiling tiling, FormatFeatureFlags features)
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

		static bool HasStencilComponent(Format format)
		{
			return format == Format.D32SfloatS8Uint || format == Format.D24UnormS8Uint;
		}
		
		static unsafe CommandBuffer BeginSingleShotCommands(VkContext context, CommandPool commandPool)
		{
			CommandBuffer commandBuffer = CreateCommandBuffer(context, commandPool);

			CommandBufferBeginInfo beginInfo = new();
			beginInfo.SType = StructureType.CommandBufferBeginInfo;
			beginInfo.Flags = CommandBufferUsageFlags.OneTimeSubmitBit;

			context.vk.BeginCommandBuffer(commandBuffer, beginInfo);

			return commandBuffer;
		}

		static unsafe void EndSingleShotCommands(VkContext context, CommandBuffer commandBuffer, CommandPool commandPool, Queue queue)
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

		static unsafe void CopyBuffer(VkContext context, CommandPool commandPool, Queue queue, Silk.NET.Vulkan.Buffer srcBuffer, Silk.NET.Vulkan.Buffer dstBuffer, ulong size)
		{
			CommandBuffer commandBuffer = BeginSingleShotCommands(context, commandPool);

			BufferCopy copyRegion = new();
			copyRegion.SrcOffset = 0;
			copyRegion.DstOffset = 0;
			copyRegion.Size = size;

			context.vk.CmdCopyBuffer(commandBuffer, srcBuffer, dstBuffer, 1, copyRegion);

			EndSingleShotCommands(context, commandBuffer, commandPool, queue);
		}

		static unsafe void CopyBuffer(VkContext context, CommandPool commandPool, Queue queue, Silk.NET.Vulkan.Buffer srcBuffer, Silk.NET.Vulkan.Image dstImage, uint width, uint height)
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

		static unsafe void TransitionImageLayout(VkContext context, CommandPool commandPool, Queue queue, Silk.NET.Vulkan.Image image, Format format, ImageLayout oldLayout, ImageLayout newLayout)
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

				if (HasStencilComponent(format))
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
	} 
}
