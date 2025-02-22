using System.Runtime.InteropServices;

namespace Engine
{
	[StructLayout(LayoutKind.Explicit, Pack = 16)]
	public struct GuiUniformBufferObject
	{
		[FieldOffset(0)]
		public Matrix4x4f proj;
		[FieldOffset(64)]
		public Vector2f screenSize;
		[FieldOffset(80)]
		public Vector4f position;
		[FieldOffset(96)]
		public Vector4f size;
	}

	[StructLayout(LayoutKind.Explicit, Pack = 16)]
	public struct GuiStateBufferObject
	{
		[FieldOffset(0)]
		public int totalStates;

		[FieldOffset(4)]
		public int state;
	}
}
