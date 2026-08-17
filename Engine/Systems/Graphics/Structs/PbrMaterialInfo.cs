using Engine.Graphics;
using System.Runtime.InteropServices;

namespace Engine
{
	[StructLayout(LayoutKind.Sequential, Pack = 16)]
	public struct PbrMaterialInfo
	{
		public Vector3f albedo;
		public float metallic;
		public float roughness;

		public static PbrMaterialInfo FromMaterial(in PbrMaterialBuffer material)
		{
			return new()
			{
				albedo = material.albedo,
				metallic = material.metallic,
				roughness = material.roughness
			};
		}
	}
}
