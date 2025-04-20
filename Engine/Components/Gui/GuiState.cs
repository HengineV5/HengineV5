using EnCS.Attributes;

namespace Engine.Components
{
	[Component]
	public partial struct GuiState
	{
		public int state;

		public GuiState(int state)
		{
			this.state = state;
		}
	}
}
