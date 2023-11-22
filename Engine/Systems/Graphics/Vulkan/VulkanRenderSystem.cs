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
using SixLabors.ImageSharp;
using System.Buffers;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Engine
{
	public struct UniformBufferObject
	{
		public Matrix4x4 translation;
		public Matrix4x4 rotation;
		public Matrix4x4 scale;
		public Matrix4x4 view;
		public Matrix4x4 proj;
	}

	public unsafe struct MappedMemory<T> where T : unmanaged
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

		uint indiciesLength;

		IWindow window;
		VkContext context;

		Swapchain swapchain;
		RenderPipeline renderPipeline;

		//Queue graphicsQueue;
		//Queue presentQueue;
		SurfaceKHR surface;
		//SwapchainKHR swapchain;
		//RenderPass renderPass;
		//DescriptorSetLayout descriptorSetLayout;
		//PipelineLayout pipelineLayout;
		//Pipeline pipeline;
		CommandPool commandPool;
		//DescriptorPool descriptorPool;

		Silk.NET.Vulkan.Image texture;
		DeviceMemory textureMemory;
		ImageView textureImageView;

		//Silk.NET.Vulkan.Image depthImage;
		//DeviceMemory depthImageMemory;
		//ImageView depthImageView;

		//Sampler sampler;

		//Memory<CommandBuffer> commandBuffers;

		Silk.NET.Vulkan.Buffer vertexBuffer;
		DeviceMemory vertexBufferMemory;
		Silk.NET.Vulkan.Buffer indexBuffer;
		DeviceMemory indexBufferMemory;

		//Memory<Silk.NET.Vulkan.Buffer> uniformBuffers;
		//Memory<DeviceMemory> uniformBuffersMemory;
		//Memory<MappedMemory<UniformBufferObject>> uniformBufferObjects;

		//Memory<Silk.NET.Vulkan.Image> images;
		//Memory<ImageView> imageViews;
		//Memory<Framebuffer> frameBuffers;

		//Memory<Silk.NET.Vulkan.Semaphore> imageAvailableSemaphores;
		//Memory<Silk.NET.Vulkan.Semaphore> renderFinishedSemaphores;
		//Memory<Fence> inFlightFences;

		//Memory<DescriptorSet> descriptorSets;

		//int currentFrame = 0;
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

			surface = CreateSurface(context);
			swapchain = Swapchain.Create(context, surface);

			commandPool = VulkanHelper.CreateCommandPool(context, swapchain.GetGraphicsQueueFamily());

			vertexBuffer = VulkanHelper.CreateBuffer<Vertex>(context, BufferUsageFlags.TransferDstBit | BufferUsageFlags.VertexBufferBit, (uint)verticies.Length);
			vertexBufferMemory = VulkanHelper.CreateBufferMemory(context, vertexBuffer, MemoryPropertyFlags.DeviceLocalBit);
			indexBuffer = VulkanHelper.CreateBuffer<ushort>(context, BufferUsageFlags.TransferDstBit | BufferUsageFlags.IndexBufferBit, (uint)indicies.Length);
			indexBufferMemory = VulkanHelper.CreateBufferMemory(context, indexBuffer, MemoryPropertyFlags.DeviceLocalBit);

			Silk.NET.Vulkan.Buffer stagingBuffer = VulkanHelper.CreateBuffer<Vertex>(context, BufferUsageFlags.TransferSrcBit, (uint)verticies.Length);
			DeviceMemory stagingBufferMemory = VulkanHelper.CreateBufferMemory(context, stagingBuffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

			VulkanHelper.MapBufferMemory(context, stagingBuffer, stagingBufferMemory, verticies.Span);
			VulkanHelper.CopyBuffer(context, commandPool, swapchain.GetGraphicsQueue(), stagingBuffer, vertexBuffer, (uint)verticies.Span.Length * Vertex.SizeInBytes);

			VulkanHelper.MapBufferMemory(context, stagingBuffer, stagingBufferMemory, indicies.Span);
			VulkanHelper.CopyBuffer(context, commandPool, swapchain.GetGraphicsQueue(), stagingBuffer, indexBuffer, (uint)indicies.Span.Length * sizeof(ushort));

			unsafe
			{
				context.vk.DestroyBuffer(context.device, stagingBuffer, null);
				context.vk.FreeMemory(context.device, stagingBufferMemory, null);
			}

			using var img = SixLabors.ImageSharp.Image.Load<Rgba32>("Images/viking_room.png");
			int imageSize = img.Width * img.Height * img.PixelType.BitsPerPixel / 8;

			texture = VulkanHelper.CreateImage(context, new((uint)img.Width, (uint)img.Height), Format.R8G8B8A8Srgb, ImageTiling.Optimal, ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit);
			textureMemory = VulkanHelper.CreateMemory(context, texture, MemoryPropertyFlags.DeviceLocalBit);
			textureImageView = VulkanHelper.CreateImageView(context, texture, Format.R8G8B8A8Srgb, ImageAspectFlags.ColorBit);

			using var buff = MemoryPool<byte>.Shared.Rent(imageSize);
			img.CopyPixelDataTo(buff.Memory.Span);

			stagingBuffer = VulkanHelper.CreateBuffer<byte>(context, BufferUsageFlags.TransferSrcBit, (uint)imageSize);
			stagingBufferMemory = VulkanHelper.CreateBufferMemory(context, stagingBuffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

			VulkanHelper.MapBufferMemory(context, stagingBuffer, stagingBufferMemory, buff.Memory.Span);

			VulkanHelper.TransitionImageLayout(context, commandPool, swapchain.GetGraphicsQueue(), texture, Format.R8G8B8A8Srgb, ImageLayout.Undefined, ImageLayout.TransferDstOptimal);
			VulkanHelper.CopyBuffer(context, commandPool, swapchain.GetGraphicsQueue(), stagingBuffer, texture, (uint)img.Width, (uint)img.Height);
			VulkanHelper.TransitionImageLayout(context, commandPool, swapchain.GetGraphicsQueue(), texture, Format.R8G8B8A8Srgb, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);

			unsafe
			{
				context.vk.DestroyBuffer(context.device, stagingBuffer, null);
				context.vk.FreeMemory(context.device, stagingBufferMemory, null);
			}

			renderPipeline = RenderPipeline.Create(context, swapchain, surface, textureImageView, commandPool);

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

			/*
			Format depthFormat = FindSupportedFormat(context, [Format.D32Sfloat, Format.D32SfloatS8Uint, Format.D24UnormS8Uint], ImageTiling.Optimal, FormatFeatureFlags.DepthStencilAttachmentBit);

			var graphicsQueueFamily = GetGraphicsQueueFamily(context);
			swapchain = CreateSwapChain(context, surface, graphicsQueueFamily);

			var extent = ChooseSwapExtent(context, surface);
            images = GetSwapChainImages(context, swapchain);
			var surfaceFormat = ChooseSwapSurfaceFormat(context, surface);
			imageViews = VulkanHelper.CreateImageViews(context, images.Span, surfaceFormat.Format, ImageAspectFlags.ColorBit);

			depthImage = CreateTextureImage(context, extent, depthFormat, ImageTiling.Optimal, ImageUsageFlags.DepthStencilAttachmentBit);
			depthImageMemory = CreateImageMemory(context, depthImage, MemoryPropertyFlags.DeviceLocalBit);
			depthImageView = CreateImageView(context, depthImage, depthFormat, ImageAspectFlags.DepthBit);

			renderPass = CreateRenderPass(context, surface, depthFormat);
			frameBuffers = CreateFrameBuffers(context, imageViews.Span, depthImageView, renderPass, extent);
			pipelineLayout = CreatePipelineLayout(context, descriptorSetLayout);

			pipeline = CreateGraphicsPipeline(context, extent, pipelineLayout, renderPass);
			commandBuffers = CreateCommandBuffers(context, commandPool, MAX_FRAMES_IN_FLIGHT);
			*/
		}

		public unsafe void Dispose()
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

		public unsafe void PreRun()
		{
            window.DoEvents();

            Thread.Sleep(10);
		}

		public unsafe void Update(Position.Ref position, Rotation.Ref rotation, Scale.Ref scale)
		{
			UniformBufferObject ubo = new();
			UniformBufferUpdate(ref ubo, swapchain.GetExtent());

			renderPipeline.Render(context, ref ubo, vertexBuffer, indexBuffer, indiciesLength);
		}

		public void PostRun()
		{

		}

		unsafe void DisposeSwapChain()
		{
			/*
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
			*/
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

		static unsafe SurfaceKHR CreateSurface(VkContext context)
		{
			return context.window.VkSurface.Create<AllocationCallbacks>(context.instance.ToHandle(), null).ToSurface();
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
			surfaceFormats = VulkanHelper.GetSurfaceFormats(surfaceFormats, khrSurface, context.physicalDevice, surface);

			Span<PresentModeKHR> presentModees = stackalloc PresentModeKHR[16];
			presentModees = VulkanHelper.GetSurfacePresentModes(presentModees, khrSurface, context.physicalDevice, surface);

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
			qFamilies = VulkanHelper.GetQueueFamilies(context, qFamilies, context.physicalDevice);

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
	} 
}
