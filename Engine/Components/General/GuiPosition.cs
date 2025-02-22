using EnCS.Attributes;

namespace Engine.Components
{
	[Component]
	public partial struct GuiPosition
	{
		public float x;
		public float y;
		public float z;
		public float w;

		public GuiPosition(float x, float y, float z, float w)
		{
			this.x = x;
			this.y = y;
			this.z = z;
			this.w = w;
		}

		public static implicit operator GuiPosition(Vector4f v) => new(v.x, v.y, v.z, v.w);
	}
}
