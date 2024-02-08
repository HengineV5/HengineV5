using EnCS;
using EnCS.Attributes;
using System.Numerics;

namespace Engine.Graphics
{
    public struct VkPbrMaterial
    {
        public Vector3 albedo;
        public VkTextureBuffer albedoMap;
        public float metallic;
        public VkTextureBuffer metallicMap;
        public float roughness;
        public VkTextureBuffer roughnessMap;
        public VkTextureBuffer aoMap;
        public VkTextureBuffer normalMap;
    }

    [ResourceManager]
    public partial class VulkanMaterialResourceManager : IResourceManager<PbrMaterial, VkPbrMaterial>
    {
        uint idx = 0;
        Memory<Graphics.VkPbrMaterial> materialBuffers = new Graphics.VkPbrMaterial[32];

        Dictionary<string, uint> materialCache = new Dictionary<string, uint>();

        VkContext context;

        public VulkanMaterialResourceManager(VkContext context)
        {
            this.context = context;
        }

        public ref VkPbrMaterial Get(uint id)
        {
            return ref materialBuffers.Span[(int)id];
        }

        public uint Store(in Graphics.PbrMaterial resource)
        {
            if (materialCache.TryGetValue(resource.name, out uint id))
                return id;

            materialCache.Add(resource.name, idx);
            materialBuffers.Span[(int)idx] = CreateMaterialBuffer(context, resource);
            return idx++;
        }

        public static VkPbrMaterial CreateMaterialBuffer(VkContext context, Graphics.PbrMaterial material)
        {
            return new VkPbrMaterial()
            {
                albedo = material.albedo,
                albedoMap = VulkanTextureResourceManager.CreateTextureBuffer(context, material.albedoMap),
                metallic = material.metallic,
                metallicMap = VulkanTextureResourceManager.CreateTextureBuffer(context, material.metallicMap),
                roughness = material.roughness,
                roughnessMap = VulkanTextureResourceManager.CreateTextureBuffer(context, material.roughnessMap),
                aoMap = VulkanTextureResourceManager.CreateTextureBuffer(context, material.aoMap),
                normalMap = VulkanTextureResourceManager.CreateTextureBuffer(context, material.normalMap),
            };
        }
    }
}
