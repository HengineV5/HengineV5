using EnCS.Attributes;
using System.Numerics;

namespace Engine.Components
{
	[Component]
	public partial struct Position
	{
		public float x;
		public float y;
		public float z;

		public Position(float x, float y, float z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public static implicit operator Position(Vector3 v) => new(v.X, v.Y, v.Z);
		public static implicit operator Position(Vector2 v) => new(v.X, v.Y, 0);
	}
}
