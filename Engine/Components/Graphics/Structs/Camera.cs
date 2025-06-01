using EnCS.Attributes;

namespace Engine.Graphics
{
	[Component]
	public ref partial struct Camera
	{
		public ref float zNear;
		public ref float zFar;
		public ref float fov;
		public ref float width;
		public ref float height;
	}
}
