using EnCS.Attributes;
using System.Numerics;

namespace Hengine.Components
{
	[Component]
	public ref partial struct Rotation
	{
		public ref float x;
		public ref float y;
		public ref float z;
		public ref float w;

		public static implicit operator Quaternionf(Rotation v) => new(v.x, v.y, v.z, v.w);
		public static implicit operator Vector4f(Rotation v) => new(v.x, v.y, v.z, v.w);
		public static implicit operator Vector3f(Rotation v) => new(v.x, v.y, v.z);
		public static implicit operator Vector2f(Rotation v) => new(v.x, v.y);
	}

	static partial class Comp_Extensions
	{
		public static void Set(this ref Rotation position, Quaternionf value)
		{
			position.x = value.x;
			position.y = value.y;
			position.z = value.z;
			position.w = value.w;
		}

		public static void Set(this ref Rotation position, Vector4f value)
		{
			position.x = value.x;
			position.y = value.y;
			position.z = value.z;
			position.w = value.w;
		}

		public static void Set(this ref Rotation position, Vector3f value)
		{
			position.x = value.x;
			position.y = value.y;
			position.z = value.z;
			position.w = 0;
		}

		public static void Set(this ref Rotation position, Vector2f value)
		{
			position.x = value.x;
			position.y = value.y;
			position.z = 0;
			position.w = 0;
		}
	}
}
