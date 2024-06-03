using System.Numerics;
using System.Runtime.InteropServices;

namespace Engine
{
	[StructLayout(LayoutKind.Sequential, Pack = 16)]
	public struct MeshUniformBufferObject
	{
		public Matrix4x4 translation;
		public Matrix4x4 rotation;
		public Matrix4x4 scale;
		public Matrix4x4 view;
		public Matrix4x4 proj;
		public Vector3 cameraPos;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 16)]
	public struct GizmoUniformBufferObject
	{
		public Vector3 color;
	}
}
