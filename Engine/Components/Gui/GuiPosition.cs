using EnCS.Attributes;

namespace Engine.Components
{
	[Component]
	public partial struct GuiPosition
	{
		public ref float x;
		public ref float y;
		public ref float z;
		public ref float w;

		public static implicit operator Vector4f(GuiPosition v) => new(v.x, v.y, v.z, v.w);
	}

	public static partial class GuiPosition_Extensions
	{
		public static void Set(this ref GuiPosition position, Vector4f value)
		{
			position.x = value.x;
			position.y = value.y;
			position.z = value.z;
			position.w = value.w;
		}
	}
}
