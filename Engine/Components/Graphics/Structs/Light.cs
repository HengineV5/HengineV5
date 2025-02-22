using System.Runtime.InteropServices;

namespace Engine.Graphics
{
	[StructLayout(LayoutKind.Sequential, Pack = 16)]
	public struct Light
	{
		public Vector3f Position;
		public Vector3f Ambient;
		public Vector3f Diffuse;
		public Vector3f Specular;
	}
}
