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

			var backend = renderContext.CreateBackend();
			atlasBuffers.Span[(int)idx] = CreateAtlasBuffer(ref backend, resource);

			return idx++;
		}

		public static TextureAtlasBuffer CreateAtlasBuffer<TBackend>(ref TBackend backend, Graphics.TextureAtlas atlas) where TBackend : struct, IRenderBackend<TBackend>, allows ref struct
		{
			return new TextureAtlasBuffer()
			{
				textures = atlas.textures,
				atlas = TextureBufferFactory.CreateTextureBuffer(ref backend, atlas.textureAtlas)
			};
		}
	}
}
