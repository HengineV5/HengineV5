using System.Runtime.InteropServices;

namespace Hengine
{
	[StructLayout(LayoutKind.Sequential, Pack = 16)]
	public struct MeshUniformBufferObject
	{
		public Matrix4x4f translation;
		public Matrix4x4f rotation;
		public Matrix4x4f scale;
		public Matrix4x4f view;
		public Matrix4x4f proj;
		public Vector3f cameraPos;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 16)]
	public struct GizmoUniformBufferObject
	{
		public Vector3f color;
	}
}
