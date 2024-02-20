using System.Numerics;
using System.Runtime.InteropServices;

namespace Engine
{
	[StructLayout(LayoutKind.Explicit)]
	public struct UniformBufferObject
	{
		[FieldOffset(0)]
		public Matrix4x4 translation;

		[FieldOffset(64)]
		public Matrix4x4 rotation;

		[FieldOffset(128)]
		public Matrix4x4 scale;

		[FieldOffset(192)]
		public Matrix4x4 view;

		[FieldOffset(256)]
		public Matrix4x4 proj;

		[FieldOffset(320)]
		public Vector3 cameraPos;
	}
}
