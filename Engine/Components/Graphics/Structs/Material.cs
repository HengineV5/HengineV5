using System.Numerics;
using System.Runtime.InteropServices;

namespace Engine.Graphics
{
	// Phong
	[StructLayout(LayoutKind.Sequential, Pack = 16)]
	public struct Material
	{
		//[FieldOffset(0)]
		public Vector3 Ambient;

		//[FieldOffset(16)]
		public Vector3 Diffuse;

		//[FieldOffset(32)]
		public Vector3 Specular;

		//[FieldOffset(44)]
		public float Shininess;

		public static readonly Material Emerald = new Material
		{
			Ambient = new Vector3(0.0215f, 0.1745f, 0.0215f),
			Diffuse = new Vector3(0.07568f, 0.61424f, 0.07568f),
			Specular = new Vector3(0.633f, 0.727811f, 0.633f),
			Shininess = 0.6f
		};

		public static readonly Material WhitePlastic = new Material
		{
			Ambient = Vector3.Zero,
			Diffuse = new Vector3(0.55f, 0.55f, 0.55f),
			Specular = new Vector3(0.70f, 0.70f, 0.70f),
			Shininess = 0.25f
		};
	}

	[StructLayout(LayoutKind.Sequential, Pack = 16)]
	public struct PbrMaterial
	{
		public Vector3 albedo;
		public float metallic;
		public float roughness;
	}
}
