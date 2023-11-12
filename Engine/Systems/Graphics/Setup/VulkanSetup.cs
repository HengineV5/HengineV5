using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.SDL;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using System.Runtime.InteropServices;

namespace Engine
{
	public class VulkanConfig
	{
		public string[] validationLayers;
	}

	public class VkContext
	{
		static string[] DEVICE_EXTENSIONS =
		[
			KhrSwapchain.ExtensionName,
		];

		Vk vk;
		IWindow window;

		Instance instance;
		PhysicalDevice physicalDevice;
		Device device;

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
				throw new Exception("Failed to create vkInstance");

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
				throw new Exception("Failed to create vkDevice");

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

	public static class VulkanSetup
	{
		public static VkContext ContextSetup(Vk vk, IWindow window, EngineConfig engineConfig, VulkanConfig vulkanConfig)
		{
			VkContext vkContext = new VkContext(vk, window);
			vkContext.Setup(engineConfig, vulkanConfig);

			return vkContext;
		}
	}
}
