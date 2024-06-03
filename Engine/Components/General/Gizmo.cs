using EnCS.Attributes;

namespace Engine.Components
{
	public enum GizmoType
	{
		Point,
		Arrow,
	}

	public struct GizmoColor
	{
		public float R;
		public float G;
		public float B;

		public GizmoColor(float r, float g, float b)
		{
			R = r;
			G = g;
			B = b;
		}
	}

	[Component]
	public partial struct GizmoComp
	{
		public GizmoType type;
		public GizmoColor color;
	}

	public struct GizmoLinePosition
	{
		public float x;
		public float y;
		public float z;

		public GizmoLinePosition(float x, float y, float z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}
	}

	[Component]
	public partial struct GizmoLine
	{
		public GizmoLinePosition p1;
		public GizmoLinePosition p2;

		public GizmoColor color;
	}
}
