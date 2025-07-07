using Hengine.Graphics;

namespace Hengine
{
	public struct VulkanRenderContext
	{
		public MeshUniformBufferObject pbrUbo;
		public MeshUniformBufferObject skyboxUbo;
		public GuiUniformBufferObject guiUbo;
		public MeshUniformBufferObject gizmoUbo;
		public VkSkybox skybox;

		public VulkanRenderContext()
		{
			this.pbrUbo = new MeshUniformBufferObject();
		}
	}
}
