using EnCS;
using EnCS.Attributes;

namespace Engine.Graphics
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

		public VulkanTextureAtlasResourceManager(VkContext context)
		{
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
