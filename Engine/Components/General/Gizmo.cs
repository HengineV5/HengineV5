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

		public GizmoColor(float v)
		{
			R = v;
			G = v;
			B = v;
		}
	}

	[Component]
	public ref partial struct GizmoComp
	{
		public ref GizmoType type;
		public ref GizmoColor color;
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
	public ref partial struct GizmoLine
	{
		public ref GizmoLinePosition p1;
		public ref GizmoLinePosition p2;

		public ref GizmoColor color;
	}
}
