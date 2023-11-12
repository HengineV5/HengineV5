using EnCS.Attributes;
using System.Numerics;

namespace Engine.Components
{
	[Component]
	public partial struct Scale
	{
		public float x;
		public float y;
		public float z;

		public Scale(float x, float y, float z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public static implicit operator Scale(Vector3 v) => new(v.X, v.Y, v.Z);
		public static implicit operator Scale(Vector2 v) => new(v.X, v.Y, 0);
	}
}
