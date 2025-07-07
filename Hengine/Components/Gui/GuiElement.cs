using EnCS.Attributes;

namespace Hengine.Components
{
	public enum GuiShape
	{
		Box,
		Circle
	}

	[Component]
	public ref partial struct GuiProperties
	{
		public ref GuiShape shape;
		public ref float z;
	}
}
