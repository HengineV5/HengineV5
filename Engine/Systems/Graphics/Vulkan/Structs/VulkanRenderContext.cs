using Engine.Graphics;

namespace Engine
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
