using EnCS.Attributes;
using System.Numerics;

namespace Engine.Components
{
	[Component]
	public partial struct Size
	{
		public float x;
		public float y;

		public Size(float x, float y)
		{
			this.x = x;
			this.y = y;
		}

		public static implicit operator Size(Vector2 v) => new(v.X, v.Y);
	}
}
