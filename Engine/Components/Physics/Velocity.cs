using EnCS.Attributes;

namespace Engine.Primitives
{
	[Component]
	public ref partial struct Velocity
	{
		public ref float x;
		public ref float y;
		public ref float z;
	}
}
