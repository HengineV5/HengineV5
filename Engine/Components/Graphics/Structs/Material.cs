using Engine.Parsing.Gltf;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Engine.Graphics
{
	// Phong
	[StructLayout(LayoutKind.Sequential, Pack = 16)]
	public struct Material
	{
		public Vector3 Ambient;
		public Vector3 Diffuse;
		public Vector3 Specular;
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

    public struct PbrMaterialNew
    {
		public string name;

        public Vector3 albedo;
        public ETexture albedoMap;
        public float metallic;
        public ETexture metallicMap;
        public float roughness;
        public ETexture roughnessMap;
        public ETexture aoMap;

        public static PbrMaterialNew LoadGltf(string name, string filePath)
        {
            PbrMaterialNew material = new PbrMaterialNew();
			material.albedo = Vector3.One;

            GltfLoader.LoadMaterial(name, filePath, ref material);

			if (string.IsNullOrEmpty(material.albedoMap.name))
				material.albedoMap = ETexture.LoadImage($"{name}_albedo", "Images/Pbr/Default/Albedo.png");

			if (string.IsNullOrEmpty(material.metallicMap.name))
				material.metallicMap = ETexture.LoadImage($"{name}_metallic", "Images/Pbr/Default/Metallic.png");

			if (string.IsNullOrEmpty(material.roughnessMap.name))
				material.roughnessMap = ETexture.LoadImage($"{name}_roughness", "Images/Pbr/Default/Roughness.png");

			if (string.IsNullOrEmpty(material.aoMap.name))
				material.aoMap = ETexture.LoadImage($"{name}_ao", "Images/Pbr/Default/Ao.png");

            return material;
        }
    }
}
