using EnCS.Attributes;
using System.Numerics;

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

		public static implicit operator GuiPosition(Vector4 v) => new(v.X, v.Y, v.Z, v.W);
	}
}
