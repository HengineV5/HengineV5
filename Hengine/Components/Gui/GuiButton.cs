using EnCS.Attributes;

namespace Hengine.Components
{
	[Component]
	public ref partial struct GuiButton
	{
		public ref int normalState;
		public ref int hoverState;
		public ref int pressedState;
	}
}
