using System.Numerics;
using System.Runtime.InteropServices;

namespace Engine.Graphics
{
	[StructLayout(LayoutKind.Sequential, Pack = 16)]
	public struct Light
	{
		public Vector3 Position;
		public Vector3 Ambient;
		public Vector3 Diffuse;
		public Vector3 Specular;
	}
}
