using EnCS.Attributes;

namespace Engine.Components
{
	public enum GuiShape
	{
		Box,
		Circle
	}

	[Component]
	public partial struct GuiProperties
	{
		public GuiShape shape;
		public float z;

		public GuiProperties(GuiShape shape, float z)
		{
			this.shape = shape;
			this.z = z;
		}
	}
}
