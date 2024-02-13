using EnCS.Attributes;

namespace Engine.Components
{
	public enum GizmoType
	{
		Point,
	}

	[Component]
	public partial struct GizmoComp
	{
		public GizmoType gizmoType;
	}
}
