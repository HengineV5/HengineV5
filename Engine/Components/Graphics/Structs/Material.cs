using Engine.Utils.Parsing.GLTF;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Engine.Graphics
{
    public struct PbrMaterial
    {
		public string name;

        public Vector3 albedo;
        public ETexture albedoMap;
        public float metallic;
        public ETexture metallicMap;
        public float roughness;
        public ETexture roughnessMap;
        public ETexture aoMap;
        public ETexture normalMap;
        public ETexture depthMap;

        public static PbrMaterial LoadGltf(string name, string filePath)
        {
            PbrMaterial material = new PbrMaterial();
			material.albedo = Vector3.One;
            material.metallic = 1;
            material.roughness = 1;

            GltfLoader.LoadMaterial(name, filePath, ref material);

			if (string.IsNullOrEmpty(material.albedoMap.name))
				material.albedoMap = ETexture.LoadImage($"{name}_albedo", "Images/Pbr/Default/Albedo.png");

			if (string.IsNullOrEmpty(material.metallicMap.name))
				material.metallicMap = ETexture.LoadImage($"{name}_metallic", "Images/Pbr/Default/Metallic.png");

			if (string.IsNullOrEmpty(material.roughnessMap.name))
				material.roughnessMap = ETexture.LoadImage($"{name}_roughness", "Images/Pbr/Default/Roughness.png");

			if (string.IsNullOrEmpty(material.aoMap.name))
				material.aoMap = ETexture.LoadImage($"{name}_ao", "Images/Pbr/Default/Ao.png");

			if (string.IsNullOrEmpty(material.normalMap.name))
				material.normalMap = ETexture.LoadImage($"{name}_normal", "Images/Pbr/Default/Normal.png");

			if (string.IsNullOrEmpty(material.depthMap.name))
				material.normalMap = ETexture.LoadImage($"{name}_depth", "Images/Pbr/Default/Depth.png");

            return material;
        }

        public static PbrMaterial GetDefault(string name)
        {
            PbrMaterial material = new PbrMaterial();
            material.name = name;
            material.albedo = Vector3.One;
            material.albedoMap = ETexture.LoadImage($"{name}_albedo", "Images/Pbr/Default/Albedo.png");
            material.metallicMap = ETexture.LoadImage($"{name}_metallic", "Images/Pbr/Default/Metallic.png");
            material.roughnessMap = ETexture.LoadImage($"{name}_roughness", "Images/Pbr/Default/Roughness.png");
            material.aoMap = ETexture.LoadImage($"{name}_ao", "Images/Pbr/Default/Ao.png");
            material.normalMap = ETexture.LoadImage($"{name}_normal", "Images/Pbr/Default/Normal.png");
            material.depthMap = ETexture.LoadImage($"{name}_depth", "Images/Pbr/Default/Depth.png");

            return material;
        }
    }
}
