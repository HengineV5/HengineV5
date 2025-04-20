using EnCS.Attributes;

namespace Engine.Components
{
	[Component]
	public partial struct GuiButton
	{
		public int normalState = 0;
		public int hoverState = 1;
		public int pressedState = 2;

		public GuiButton()
		{
			
		}
	}
}
