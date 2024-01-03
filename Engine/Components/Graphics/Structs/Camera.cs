using EnCS.Attributes;

namespace Engine.Graphics
{
	[Component]
	public partial struct Camera
	{
		public float zNear;
		public float zFar;
		public float fov;
		public float width;
		public float height;
	}
}
