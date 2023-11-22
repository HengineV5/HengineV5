using Silk.NET.Vulkan;

namespace Engine
{
	public static class VulkanExtensions
	{
		public static bool HasStencilComponent(this Format format)
		{
			return format == Format.D32SfloatS8Uint || format == Format.D24UnormS8Uint;
		}
	}
}
