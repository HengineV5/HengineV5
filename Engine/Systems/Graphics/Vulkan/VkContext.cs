using Silk.NET.Core;
using Silk.NET.Core.Contexts;
using Silk.NET.Core.Native;
using Silk.NET.SDL;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using System.Buffers;
using System.Runtime.InteropServices;
using Image = Silk.NET.Vulkan.Image;

namespace Engine
{
	public class VkRenderContext
	{
		VkContext context;

		public SurfaceKHR surface;
		public CommandPool commandPool;

		public RenderPipeline renderPipeline;

		public VkRenderContext(VkContext context)
        {
			this.context = context;
        }

		public void Setup()
		{
			uint graphicsQueueFamily = VulkanHelper.GetGraphicsQueueFamily(context);
			Queue graphicsQueue = VulkanHelper.GetQueue(context, graphicsQueueFamily);

			surface = CreateSurface(context);
			commandPool = VulkanHelper.CreateCommandPool(context, graphicsQueueFamily);

			using var img = SixLabors.ImageSharp.Image.Load<Rgba32>("Images/image_2.png");
			int imageSize = img.Width * img.Height * img.PixelType.BitsPerPixel / 8;

			Image texture = VulkanHelper.CreateImage(context, new((uint)img.Width, (uint)img.Height), Format.R8G8B8A8Srgb, ImageTiling.Optimal, ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit);
			DeviceMemory textureMemory = VulkanHelper.CreateMemory(context, texture, MemoryPropertyFlags.DeviceLocalBit);
			ImageView textureImageView = VulkanHelper.CreateImageView(context, texture, Format.R8G8B8A8Srgb, ImageAspectFlags.ColorBit);

			using var buff = MemoryPool<byte>.Shared.Rent(imageSize);
			img.CopyPixelDataTo(buff.Memory.Span);

			Silk.NET.Vulkan.Buffer stagingBuffer = VulkanHelper.CreateBuffer<byte>(context, BufferUsageFlags.TransferSrcBit, (uint)imageSize);
			DeviceMemory stagingBufferMemory = VulkanHelper.CreateBufferMemory(context, stagingBuffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

			VulkanHelper.MapBufferMemory(context, stagingBuffer, stagingBufferMemory, buff.Memory.Span);

			VulkanHelper.TransitionImageLayout(context, commandPool, graphicsQueue, texture, Format.R8G8B8A8Srgb, ImageLayout.Undefined, ImageLayout.TransferDstOptimal);
			VulkanHelper.CopyBuffer(context, commandPool, graphicsQueue, stagingBuffer, texture, (uint)img.Width, (uint)img.Height);
			VulkanHelper.TransitionImageLayout(context, commandPool, graphicsQueue, texture, Format.R8G8B8A8Srgb, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);

			unsafe
			{
				context.vk.DestroyBuffer(context.device, stagingBuffer, null);
				context.vk.FreeMemory(context.device, stagingBufferMemory, null);
			}

			renderPipeline = RenderPipeline.Create(context, surface, textureImageView, commandPool);
		}

		static unsafe SurfaceKHR CreateSurface(VkContext context)
		{
			return context.window.VkSurface.Create<AllocationCallbacks>(context.instance.ToHandle(), null).ToSurface();
		}
	}

	public class VkContext
	{
		static string[] DEVICE_EXTENSIONS =
		[
			KhrSwapchain.ExtensionName,
			//KhrSurface.ExtensionName,
			KhrPushDescriptor.ExtensionName,
		];

		public Vk vk;
		public IWindow window;

		public Instance instance;
		public PhysicalDevice physicalDevice;
		public Device device;

		public KhrSurface surface;
		public KhrSwapchain swapchain;

		public VkContext(Vk vk, IWindow window)
		{
			this.vk = vk;
			this.window = window;
		}

		public void Setup(EngineConfig engineConfig, VulkanConfig vulkanConfig)
		{
			instance = CreateInstance(vk, window, vulkanConfig.validationLayers, engineConfig);
			physicalDevice = PickPhysicalDevice(vk, instance);
			var graphicsQueueFamily = GetGraphicsQueueFamily(vk, physicalDevice);
			device = CreateLogicalDevice(vk, physicalDevice, graphicsQueueFamily, vulkanConfig.validationLayers, DEVICE_EXTENSIONS);

            vk.GetPhysicalDeviceProperties(physicalDevice, out PhysicalDeviceProperties properties);
            Console.WriteLine(properties.Limits.MinUniformBufferOffsetAlignment);

            // Do not use TryGetInstanceExtension or TryGetDeviceExtensions since they use reflection.
            surface = new KhrSurface(new LamdaNativeContext((string x) => (nint)vk.GetInstanceProcAddr(instance, x)));
			swapchain = new KhrSwapchain(new LamdaNativeContext((string x) => (nint)vk.GetDeviceProcAddr(device, x)));
		}

		static unsafe Instance CreateInstance(Vk vk, IWindow window, string[] validationLayers, EngineConfig engineConfig)
		{
			ApplicationInfo appInfo = new();
			appInfo.SType = StructureType.ApplicationInfo;
			appInfo.PApplicationName = (byte*)Marshal.StringToHGlobalAnsi(engineConfig.appName);
			appInfo.ApplicationVersion = new Version32((uint)engineConfig.appVersion.Major, (uint)engineConfig.appVersion.Minor, (uint)engineConfig.appVersion.Build);
			appInfo.PEngineName = (byte*)Marshal.StringToHGlobalAnsi(engineConfig.engineName);
			appInfo.EngineVersion = new Version32((uint)engineConfig.engineVersion.Major, (uint)engineConfig.engineVersion.Minor, (uint)engineConfig.engineVersion.Build);
			appInfo.ApiVersion = Vk.Version13;

			InstanceCreateInfo createInfo = new();
			createInfo.SType = StructureType.InstanceCreateInfo;
			createInfo.PApplicationInfo = &appInfo;

			var glfwExtensions = window.VkSurface.GetRequiredExtensions(out var count);
			createInfo.EnabledExtensionCount = count;
			createInfo.PpEnabledExtensionNames = glfwExtensions;

			/*
			string[] strArr = new string[count];
			SilkMarshal.CopyPtrToStringArray((nint)glfwExtensions, strArr);
            Console.WriteLine("Instance Extensions:");
            for (int i = 0; i < count; i++)
			{
				Console.WriteLine($"\t{strArr[i]}");
            }
			*/

			if (validationLayers.Length > 0)
			{
				createInfo.EnabledLayerCount = (uint)validationLayers.Length;
				createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(validationLayers);
			}
			else
			{
				createInfo.EnabledLayerCount = 0;
			}

			var result = vk.CreateInstance(createInfo, null, out Instance instance);
			if (result != Result.Success)
				throw new Exception($"Failed to create vkInstance, {result}");

			Marshal.FreeHGlobal((IntPtr)appInfo.PApplicationName);
			Marshal.FreeHGlobal((IntPtr)appInfo.PEngineName);

			return instance;
		}

		static unsafe Device CreateLogicalDevice(Vk vk, PhysicalDevice physicalDevice, uint queueFamily, string[] validationLayers, string[] extensions)
		{
			float queuePriority = 1f;

			DeviceQueueCreateInfo queueCreateInfo = new();
			queueCreateInfo.SType = StructureType.DeviceQueueCreateInfo;
			queueCreateInfo.QueueFamilyIndex = queueFamily;
			queueCreateInfo.QueueCount = 1;
			queueCreateInfo.PQueuePriorities = &queuePriority;

			PhysicalDeviceFeatures deviceFeatures = new();
			deviceFeatures.SamplerAnisotropy = true;

			DeviceCreateInfo createInfo = new();
			createInfo.SType = StructureType.DeviceCreateInfo;
			createInfo.PQueueCreateInfos = &queueCreateInfo;
			createInfo.QueueCreateInfoCount = 1;
			createInfo.PEnabledFeatures = &deviceFeatures;

			if (validationLayers.Length > 0)
			{
				createInfo.EnabledLayerCount = (uint)validationLayers.Length;
				createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(validationLayers);
			}
			else
			{
				createInfo.EnabledLayerCount = 0;
			}

			if (extensions.Length > 0)
			{
				createInfo.EnabledExtensionCount = (uint)extensions.Length;
				createInfo.PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(extensions);
			}
			else
			{
				createInfo.EnabledLayerCount = 0;
			}

			var result = vk.CreateDevice(physicalDevice, createInfo, null, out Device device);
			if (result != Result.Success)
				throw new Exception($"Failed to create vkDevice, {result}");

			return device;
		}

		static unsafe PhysicalDevice PickPhysicalDevice(Vk vk, Instance instance)
		{
			uint deviceCount = 0;
			vk.EnumeratePhysicalDevices(instance, &deviceCount, null);

			Span<PhysicalDevice> devices = stackalloc PhysicalDevice[(int)deviceCount];
			vk.EnumeratePhysicalDevices(instance, ref deviceCount, ref devices[0]);

			PhysicalDeviceProperties properties = new();
			PhysicalDeviceFeatures features = new();
			vk.GetPhysicalDeviceProperties(devices[0], &properties);
			vk.GetPhysicalDeviceFeatures(devices[0], &features);

			return devices[0];
		}

		static unsafe uint GetGraphicsQueueFamily(Vk vk, PhysicalDevice physicalDevice)
		{
			Span<QueueFamilyProperties> qFamilies = stackalloc QueueFamilyProperties[16];
			qFamilies = GetQueueFamilies(vk, qFamilies, physicalDevice);

			for (uint i = 0; i < qFamilies.Length; i++)
			{
				if (qFamilies[(int)i].QueueFlags.HasFlag(QueueFlags.GraphicsBit))
					return i;
			}

			throw new Exception("Queue with graphics bit not found.");
		}

		static unsafe Span<QueueFamilyProperties> GetQueueFamilies(Vk vk, Span<QueueFamilyProperties> buff, PhysicalDevice physicalDevice)
		{
			uint qFamilyCount = 0;
			vk.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, ref qFamilyCount, null);

			fixed (QueueFamilyProperties* buffPtr = buff)
			{
				vk.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, ref qFamilyCount, buffPtr);
			}

			return buff.Slice(0, (int)qFamilyCount);
		}
	}
}
