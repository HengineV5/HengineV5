using EnCS;
using EnCS.Attributes;
using Silk.NET.Vulkan;

namespace Engine.Graphics
{
	public struct VkSkybox
    {
        public VkTextureBuffer skybox;
        public VkTextureBuffer irradiance;
        public VkTextureBuffer specular;
    }

    [ResourceManager]
    public partial class VulkanSkyboxResourceManager : IResourceManager<Skybox, VkSkybox>
    {
        uint idx = 0;
        Memory<Graphics.VkSkybox> skyboxBuffers = new Graphics.VkSkybox[32];

        Dictionary<string, uint> skyboxCache = new Dictionary<string, uint>();

        VkContext context;

        public VulkanSkyboxResourceManager(VkContext context)
        {
            this.context = context;
        }

        public ref VkSkybox Get(uint id)
        {
            return ref skyboxBuffers.Span[(int)id];
        }

        public uint Store(in Graphics.Skybox resource)
        {
            if (skyboxCache.TryGetValue(resource.name, out uint id))
                return id;

            skyboxCache.Add(resource.name, idx);
            skyboxBuffers.Span[(int)idx] = CreateSkyboxBuffer(context, resource);

            return idx++;
        }

        public static VkSkybox CreateSkyboxBuffer(VkContext context, Graphics.Skybox skybox)
        {
            return new VkSkybox()
            {
                skybox = VulkanTextureResourceManager.CreateCubeTextureBuffer(context, skybox.skybox, Format.R16G16B16A16Unorm),
                irradiance = VulkanTextureResourceManager.CreateCubeTextureBuffer(context, skybox.irradiance, Format.R16G16B16A16Unorm),
                specular = VulkanTextureResourceManager.CreateMipCubeTextureBuffer(context, skybox.specular, 5, Format.R16G16B16A16Unorm)
            };
        }
    }
}
