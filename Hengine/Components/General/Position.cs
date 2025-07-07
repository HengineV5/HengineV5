using EnCS.Attributes;

namespace Hengine.Components
{
	[Component]
	public ref partial struct Position
	{
		public ref float x;
		public ref float y;
		public ref float z;

		public static implicit operator Vector3f(Position v) => new(v.x, v.y, v.z);
		public static implicit operator Vector2f(Position v) => new(v.x, v.y);
	}

	public static partial class Comp_Extensions
	{
		public static void Set(this ref Position position, Vector3f value)
		{
			position.x = value.x;
			position.y = value.y;
			position.z = value.z;
		}

		public static void Set(this ref Position position, Vector2f value)
		{
			position.x = value.x;
			position.y = value.y;
			position.z = 0;
		}
	}
}
