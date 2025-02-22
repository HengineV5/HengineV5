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

		public static implicit operator Scale(Vector3f v) => new(v.x, v.y, v.z);
		public static implicit operator Scale(Vector2f v) => new(v.x, v.y, 0);
	}
}
