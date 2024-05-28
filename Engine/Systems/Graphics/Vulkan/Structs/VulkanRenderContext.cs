using Engine.Graphics;

namespace Engine
{
	public struct VulkanRenderContext
	{
		public MeshUniformBufferObject pbrUbo;
		public GuiUniformBufferObject guiUbo;
		public VkSkybox skybox;

		public VulkanRenderContext()
		{
			this.pbrUbo = new MeshUniformBufferObject();
		}
	}
}
