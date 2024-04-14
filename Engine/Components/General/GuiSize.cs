using EnCS.Attributes;
using System.Numerics;

namespace Engine.Components
{
	[Component]
	public partial struct GuiSize
	{
		public float x;
		public float y;
		public float z;
		public float w;

		public GuiSize(float x, float y, float z, float w)
		{
			this.x = x;
			this.y = y;
			this.z = z;
			this.w = w;
		}

		public static implicit operator GuiSize(Vector4 v) => new(v.X, v.Y, v.Z, v.W);
	}
}
