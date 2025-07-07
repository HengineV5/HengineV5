using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;

namespace Hengine
{
	public class VulkanConfig
	{
		public string[] validationLayers;
	}

	public static class VulkanSetup
	{
		public static VkContext ContextSetup(Vk vk, IWindow window, EngineConfig engineConfig, VulkanConfig vulkanConfig)
		{
			VkContext vkContext = new VkContext(vk, window);
			vkContext.Setup(engineConfig, vulkanConfig);

			return vkContext;
		}

		public static (VkContext, VkRenderContext) RenderSetup(Vk vk, IWindow window, EngineConfig engineConfig, VulkanConfig vulkanConfig)
		{
			VkContext vkContext = ContextSetup(vk, window, engineConfig, vulkanConfig);
			VkRenderContext vkRenderContext = new VkRenderContext(vkContext);
			vkRenderContext.Setup();

			return (vkContext, vkRenderContext);
		}
	}
}
