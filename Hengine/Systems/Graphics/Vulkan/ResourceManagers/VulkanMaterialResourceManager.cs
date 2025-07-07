using EnCS;
using EnCS.Attributes;
using Microsoft.Extensions.Logging;
using Silk.NET.OpenGL;

namespace Hengine.Graphics
{
    public struct VkPbrMaterial
    {
        public Vector3f albedo;
        public VkTextureBuffer albedoMap;
        public float metallic;
        public VkTextureBuffer metallicMap;
        public float roughness;
        public VkTextureBuffer roughnessMap;
        public VkTextureBuffer aoMap;
        public VkTextureBuffer normalMap;
        public VkTextureBuffer depthMap;
    }

    [ResourceManager]
    public partial class VulkanMaterialResourceManager : IResourceManager<PbrMaterial, VkPbrMaterial>
    {
        uint idx = 0;
        Memory<Graphics.VkPbrMaterial> materialBuffers = new Graphics.VkPbrMaterial[256];

        Dictionary<string, uint> materialCache = new Dictionary<string, uint>();

		VkContext context;
		ILogger logger;

		public VulkanMaterialResourceManager(ILoggerFactory factory, VkContext context)
		{
			this.logger = factory.CreateLogger<VulkanMaterialResourceManager>();
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

			logger.LogResourceManagerStore(resource.name);

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
                depthMap = VulkanTextureResourceManager.CreateTextureBuffer(context, material.depthMap),
            };
        }
    }
}
