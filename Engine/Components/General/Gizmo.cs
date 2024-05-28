using EnCS.Attributes;

namespace Engine.Components
{
	public enum GizmoType
	{
		Point,
		Arrow,
	}

	[Component]
	public partial struct GizmoComp
	{
		public GizmoType gizmoType;
	}
}
