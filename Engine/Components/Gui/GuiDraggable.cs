using EnCS.Attributes;

namespace Engine.Components
{
	[Component]
	public partial struct GuiDraggable
	{
		public float offsetX;
		public float offsetY;
		public int isDragging;
	}
}
