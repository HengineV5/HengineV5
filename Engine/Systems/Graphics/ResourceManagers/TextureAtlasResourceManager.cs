using EnCS;
using EnCS.Attributes;
using Microsoft.Extensions.Logging;

using RenderLib;

namespace Engine.Graphics
{
	public struct TextureAtlasBuffer
	{
		public int textures;

		public TextureBuffer atlas;
	}

	[ResourceManager]
	public partial class TextureAtlasResourceManager : IResourceManager<TextureAtlas, TextureAtlasBuffer>
	{
		uint idx = 0;
		Memory<Graphics.TextureAtlasBuffer> atlasBuffers = new Graphics.TextureAtlasBuffer[32];

		Dictionary<string, uint> atlasCache = new Dictionary<string, uint>();

		GraphicsContext renderContext;
		ILogger logger;

		public TextureAtlasResourceManager(ILoggerFactory factory, GraphicsContext renderContext)
		{
			this.logger = factory.CreateLogger<TextureAtlasResourceManager>();
			this.renderContext = renderContext;
		}

		public ref TextureAtlasBuffer Get(uint id)
		{
			return ref atlasBuffers.Span[(int)id];
		}

		public uint Store(in Graphics.TextureAtlas resource)
		{
			if (atlasCache.TryGetValue(resource.name, out uint id))
				return id;

			logger.LogResourceManagerStore(resource.name);

			atlasCache.Add(resource.name, idx);

			var storage = renderContext.Storage;
			atlasBuffers.Span[(int)idx] = CreateAtlasBuffer(ref storage, resource);

			return idx++;
		}

		public static TextureAtlasBuffer CreateAtlasBuffer<TStorage>(ref TStorage storage, Graphics.TextureAtlas atlas) where TStorage : struct, IRenderBackend<TStorage>, allows ref struct
		{
			return new TextureAtlasBuffer()
			{
				textures = atlas.textures,
				atlas = TextureBufferFactory.CreateTextureBuffer(ref storage, atlas.textureAtlas)
			};
		}
	}
}
