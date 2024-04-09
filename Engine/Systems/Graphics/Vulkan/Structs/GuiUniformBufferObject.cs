using System.Numerics;
using System.Runtime.InteropServices;

namespace Engine
{
	[StructLayout(LayoutKind.Sequential, Pack = 16)]
	public struct GuiUniformBufferObject
	{
		public Matrix4x4 proj;
		public Vector2 screenSize;
	}
}
