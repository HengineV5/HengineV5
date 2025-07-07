using EnCS.Attributes;

namespace Hengine.Graphics
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
