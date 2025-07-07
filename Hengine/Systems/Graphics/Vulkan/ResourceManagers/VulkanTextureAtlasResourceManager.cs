using EnCS;
using EnCS.Attributes;
using Microsoft.Extensions.Logging;

namespace Hengine.Graphics
{
	public struct VkTextureAtlas
    {
        public int textures;

        public VkTextureBuffer atlas;
    }

	[ResourceManager]
	public partial class VulkanTextureAtlasResourceManager : IResourceManager<TextureAtlas, VkTextureAtlas>
    {
		uint idx = 0;
		Memory<Graphics.VkTextureAtlas> atlasBuffers = new Graphics.VkTextureAtlas[32];

		Dictionary<string, uint> atlasCache = new Dictionary<string, uint>();

		VkContext context;
		ILogger logger;

		public VulkanTextureAtlasResourceManager(ILoggerFactory factory, VkContext context)
		{
			this.logger = factory.CreateLogger<VulkanTextureAtlasResourceManager>();
			this.context = context;
		}

		public ref VkTextureAtlas Get(uint id)
		{
			return ref atlasBuffers.Span[(int)id];
		}

		public uint Store(in Graphics.TextureAtlas resource)
		{
            if (atlasCache.TryGetValue(resource.name, out uint id))
				return id;

			logger.LogResourceManagerStore(resource.name);

			atlasCache.Add(resource.name, idx);
			atlasBuffers.Span[(int)idx] = CreateAtlasBuffer(context, resource);

			return idx++;
		}

		public static VkTextureAtlas CreateAtlasBuffer(VkContext context, Graphics.TextureAtlas atlas)
		{
			return new VkTextureAtlas()
			{
				textures = atlas.textures,
				atlas = VulkanTextureResourceManager.CreateTextureBuffer(context, atlas.textureAtlas)
			};
		}
	}
}
