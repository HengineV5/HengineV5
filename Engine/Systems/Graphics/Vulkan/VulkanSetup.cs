using RenderLib;
using RenderLib.Vulkan;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;

namespace Engine
{
	public static class VulkanSetup
	{
		public static VkContext ContextSetup(Vk vk, IWindow window, EngineConfig engineConfig, VulkanConfig vulkanConfig)
		{
			VkContext vkContext = new VkContext(vk, window);
			vkContext.Setup(ToAppInfo(engineConfig), vulkanConfig);

			return vkContext;
		}

		public static (VkContext, VkRenderContext) RenderSetup(Vk vk, IWindow window, EngineConfig engineConfig, VulkanConfig vulkanConfig)
		{
			VkContext vkContext = ContextSetup(vk, window, engineConfig, vulkanConfig);
			VkRenderContext vkRenderContext = new VkRenderContext(vkContext);
			vkRenderContext.Setup();

			return (vkContext, vkRenderContext);
		}

		static AppInfo ToAppInfo(EngineConfig engineConfig)
		{
			return new AppInfo
			{
				appName = engineConfig.appName,
				appVersion = engineConfig.appVersion,
				engineName = engineConfig.engineName,
				engineVersion = engineConfig.engineVersion,
			};
		}
	}
}
