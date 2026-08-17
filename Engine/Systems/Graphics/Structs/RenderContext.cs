using Engine.Graphics;

namespace Engine
{
	public struct RenderContext
	{
		public MeshUniformBufferObject pbrUbo;
		public MeshUniformBufferObject skyboxUbo;
		public GuiUniformBufferObject guiUbo;
		public MeshUniformBufferObject gizmoUbo;
		public SkyboxBuffer skybox;

		public RenderContext()
		{
			this.pbrUbo = new MeshUniformBufferObject();
		}
	}
}
