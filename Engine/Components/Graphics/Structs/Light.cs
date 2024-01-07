using System.Numerics;
using System.Runtime.InteropServices;

namespace Engine.Graphics
{
	//[StructLayout(LayoutKind.Explicit)]
	[StructLayout(LayoutKind.Sequential, Pack = 16)]
	public struct Light
	{
		//[FieldOffset(0)]
		public Vector3 Position;

		//[FieldOffset(16)]
		public Vector3 Ambient;

		//[FieldOffset(32)]
		public Vector3 Diffuse;

		//[FieldOffset(48)]
		public Vector3 Specular;
	}
}
