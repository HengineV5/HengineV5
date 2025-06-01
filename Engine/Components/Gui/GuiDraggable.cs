using EnCS.Attributes;

namespace Engine.Components
{
	[Component]
	public ref partial struct GuiDraggable
	{
		public ref float offsetX;
		public ref float offsetY;
		public ref int isDragging;
	}
}
