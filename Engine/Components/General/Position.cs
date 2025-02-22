using EnCS.Attributes;

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

		public static implicit operator Position(Vector3f v) => new(v.x, v.y, v.z);
		public static implicit operator Position(Vector2f v) => new(v.x, v.y, 0);
	}
}
