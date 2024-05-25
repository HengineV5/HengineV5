using Engine.Graphics;

namespace Engine
{
	public struct VulkanRenderContext
	{
		public PbrUniformBufferObject pbrUbo;
		public GuiUniformBufferObject guiUbo;
		public VkSkybox skybox;

		public VulkanRenderContext()
		{
			this.pbrUbo = new PbrUniformBufferObject();
		}
	}
}
