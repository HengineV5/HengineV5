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

		public static implicit operator Rotation(Quaternionf v) => new(v.x, v.y, v.z, v.w);
		public static implicit operator Rotation(Vector4f v) => new(v.x, v.y, v.z, v.w);
		public static implicit operator Rotation(Vector3f v) => new(v.x, v.y, v.z, 0);
		public static implicit operator Rotation(Vector2f v) => new(v.x, v.y, 0, 0);
	}
}
