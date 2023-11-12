using EnCS.Attributes;
using System.Numerics;

namespace Engine.Components
{
	[Component]
	public partial struct Rotation
	{
		public float x;
		public float y;
		public float z;
		public float w;

		public Rotation(float x, float y, float z, float w)
		{
			this.x = x;
			this.y = y;
			this.z = z;
			this.w = w;
		}

		public static implicit operator Rotation(Quaternion v) => new(v.X, v.Y, v.Z, v.W);
		public static implicit operator Rotation(Vector4 v) => new(v.X, v.Y, v.Z, v.W);
		public static implicit operator Rotation(Vector3 v) => new(v.X, v.Y, v.Z, 0);
		public static implicit operator Rotation(Vector2 v) => new(v.X, v.Y, 0, 0);
	}
}
