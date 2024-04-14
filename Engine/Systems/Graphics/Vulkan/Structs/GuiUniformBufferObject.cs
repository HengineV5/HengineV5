using System.Numerics;
using System.Runtime.InteropServices;

namespace Engine
{
	[StructLayout(LayoutKind.Explicit, Pack = 16)]
	public struct GuiUniformBufferObject
	{
		[FieldOffset(0)]
		public Matrix4x4 proj;
		[FieldOffset(64)]
		public Vector2 screenSize;
		[FieldOffset(80)]
		public Vector4 position;
		[FieldOffset(96)]
		public Vector4 size;
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
