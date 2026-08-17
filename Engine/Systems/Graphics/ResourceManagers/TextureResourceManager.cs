using EnCS;
using EnCS.Attributes;
using ImageLib;
using MathLib;
using Microsoft.Extensions.Logging;
using System.Buffers;

using RenderLib;

namespace Engine.Graphics
{
	static partial class ResourceManagerLoggerExtensionMethods
	{
		[LoggerMessage(Level = LogLevel.Information, Message = "Creating resource '{name}'.")]
		public static partial void LogResourceManagerStore(this ILogger logger, string name);
	}

	public struct TextureBuffer
	{
		public GpuTexture texture;
	}

	public static class TextureBufferFactory
	{
		public static TextureBuffer CreateTextureBuffer<TBackend>(ref TBackend backend, ETexture texture) where TBackend : struct, IRenderBackend<TBackend>, allows ref struct
		{
			return CreateLayered(ref backend, [texture.data], TextureFormat.R8G8B8A8Srgb);
		}

		public static TextureBuffer CreateHdrTextureBuffer<TBackend>(ref TBackend backend, ETextureHdr texture) where TBackend : struct, IRenderBackend<TBackend>, allows ref struct
		{
			return CreateLayered(ref backend, [texture.data], TextureFormat.R16G16B16A16Unorm);
		}

		public static TextureBuffer CreateCubeTextureBuffer<TBackend>(ref TBackend backend, ECubemapHdr texture) where TBackend : struct, IRenderBackend<TBackend>, allows ref struct
		{
			return CreateLayered(ref backend, [texture.right, texture.left, texture.top, texture.bottom, texture.front, texture.back], TextureFormat.R16G16B16A16Unorm);
		}

		public static TextureBuffer CreateCrossCubeTextureBuffer<TBackend>(ref TBackend backend, ETextureHdr texture, TextureFormat format) where TBackend : struct, IRenderBackend<TBackend>, allows ref struct
		{
			return CreateCrossCubeTextureBuffer(ref backend, texture.data.Span, format);
		}

		public static TextureBuffer CreateMipCrossCubeTextureBuffer<TBackend>(ref TBackend backend, ETextureHdr texture, uint mipLevels, TextureFormat format) where TBackend : struct, IRenderBackend<TBackend>, allows ref struct
		{
			return CreateMipCrossCubeTextureBuffer(ref backend, texture.data.Span, mipLevels, format);
		}

		public static TextureBuffer CreateCrossCubeTextureBuffer<TBackend, TPixel>(ref TBackend backend, ImageSpan<TPixel> img, TextureFormat format)
			where TBackend : struct, IRenderBackend<TBackend>, allows ref struct
			where TPixel : unmanaged, IPixel<TPixel>
		{
			uint sideWidth = (uint)img.Width / 4u;
			uint sideHeight = (uint)img.Height / 3u;

			Span<TextureRegion> regions = stackalloc TextureRegion[6];
			for (uint side = 0; side < 6; side++)
			{
				regions[(int)side] = new TextureRegion(side, 0, sideWidth, sideHeight, GetSideOffset((int)side, sideWidth, sideHeight, img.Width), (uint)img.Width);
			}

			return CreateFromCross(ref backend, img, format, sideWidth, sideHeight, 1, regions);
		}

		public static TextureBuffer CreateMipCrossCubeTextureBuffer<TBackend, TPixel>(ref TBackend backend, ImageSpan<TPixel> img, uint mipLevels, TextureFormat format)
			where TBackend : struct, IRenderBackend<TBackend>, allows ref struct
			where TPixel : unmanaged, IPixel<TPixel>
		{
			int imgWidth = img.Width;
			int imgHeight = img.Height;

			if (mipLevels > 1)
				imgWidth = (int)(imgWidth * (2f / 3f));

			uint sideWidth = (uint)imgWidth / 4u;
			uint sideHeight = (uint)imgHeight / 3u;

			Span<TextureRegion> regions = stackalloc TextureRegion[(int)mipLevels * 6];
			for (uint mipLevel = 0; mipLevel < mipLevels; mipLevel++)
			{
				float mipFactor = MathF.Pow(0.5f, mipLevel);

				uint mipWidth = (uint)(sideWidth * mipFactor);
				uint mipHeight = (uint)(sideHeight * mipFactor);

				int mipStartX = mipLevel == 0 ? 0 : imgWidth;
				int mipStartY = mipLevel == 0 ? 0 : (int)(imgHeight * mipFactor);
				uint mipOffset = (uint)(mipStartX + mipStartY * img.Width);

				for (uint side = 0; side < 6; side++)
				{
					ulong offset = mipOffset + GetSideOffset((int)side, mipWidth, mipHeight, img.Width);
					regions[(int)(mipLevel * 6 + side)] = new TextureRegion(side, mipLevel, mipWidth, mipHeight, offset, (uint)img.Width);
				}
			}

			return CreateFromCross(ref backend, img, format, sideWidth, sideHeight, mipLevels, regions);
		}

		static TextureBuffer CreateFromCross<TBackend, TPixel>(ref TBackend backend, ImageSpan<TPixel> img, TextureFormat format, uint sideWidth, uint sideHeight, uint mipLevels, scoped Span<TextureRegion> regions)
			where TBackend : struct, IRenderBackend<TBackend>, allows ref struct
			where TPixel : unmanaged, IPixel<TPixel>
		{
			int bytesPerPixel = (TPixel.BitDepth / 8) * TPixel.Channels;
			int imageSize = img.Width * img.Height * bytesPerPixel;

			using var buff = MemoryPool<byte>.Shared.Rent(imageSize);
			img.CopyTo(buff.Memory.Span);

			for (int i = 0; i < regions.Length; i++)
			{
				regions[i].bufferOffset *= (ulong)bytesPerPixel;
			}

			var desc = new TextureDesc(sideWidth, sideHeight, format, 6, mipLevels);

			return new TextureBuffer { texture = TBackend.CreateTexture(ref backend, desc, buff.Memory.Span.Slice(0, imageSize), regions) };
		}

		static TextureBuffer CreateLayered<TBackend, TPixel>(ref TBackend backend, scoped Span<ImageMemory<TPixel>> imgs, TextureFormat format)
			where TBackend : struct, IRenderBackend<TBackend>, allows ref struct
			where TPixel : unmanaged, IPixel<TPixel>
		{
			int bytesPerPixel = (TPixel.BitDepth / 8) * TPixel.Channels;
			int layerSize = imgs[0].Width * imgs[0].Height * bytesPerPixel;

			using var buff = MemoryPool<byte>.Shared.Rent(layerSize * imgs.Length);

			Span<TextureRegion> regions = stackalloc TextureRegion[imgs.Length];
			for (int i = 0; i < imgs.Length; i++)
			{
				imgs[i].Span.CopyTo(buff.Memory.Span.Slice(i * layerSize));
				regions[i] = new TextureRegion((uint)i, 0, (uint)imgs[i].Width, (uint)imgs[i].Height, (ulong)(i * layerSize), 0);
			}

			var desc = new TextureDesc((uint)imgs[0].Width, (uint)imgs[0].Height, format, (uint)imgs.Length, 1);

			return new TextureBuffer { texture = TBackend.CreateTexture(ref backend, desc, buff.Memory.Span.Slice(0, layerSize * imgs.Length), regions) };
		}

		static ulong GetSideOffset(int side, uint sideWidth, uint sideHeight, int rowLength)
		{
			int offsetX;
			int offsetY;
			switch (side)
			{
				case 3:
					offsetX = 1;
					offsetY = 2;
					break;
				case 1:
					offsetX = 0;
					offsetY = 1;
					break;
				case 4:
					offsetX = 1;
					offsetY = 1;
					break;
				case 2:
					offsetX = 1;
					offsetY = 0;
					break;
				case 0:
					offsetX = 2;
					offsetY = 1;
					break;
				case 5:
					offsetX = 3;
					offsetY = 1;
					break;
				default:
					throw new Exception("Unsupported side.");
			}

			return (ulong)(offsetY * sideHeight * rowLength + offsetX * sideWidth);
		}
	}

	[ResourceManager]
	public partial class TextureResourceManager : IResourceManager<ETexture, TextureBuffer>
	{
		uint idx = 0;
		Memory<Graphics.TextureBuffer> textureBuffers = new Graphics.TextureBuffer[32];

		Dictionary<string, uint> textureCache = new Dictionary<string, uint>();

		GraphicsContext renderContext;
		ILogger logger;

		public TextureResourceManager(ILoggerFactory factory, GraphicsContext renderContext)
		{
			this.logger = factory.CreateLogger<TextureResourceManager>();
			this.renderContext = renderContext;
		}

		public ref Graphics.TextureBuffer Get(uint id)
		{
			return ref textureBuffers.Span[(int)id];
		}

		public uint Store(in Graphics.ETexture texture)
		{
			if (textureCache.TryGetValue(texture.name, out uint id))
				return id;

			logger.LogResourceManagerStore(texture.name);

			textureCache.Add(texture.name, idx);
			var backend = renderContext.CreateBackend();
			textureBuffers.Span[(int)idx] = TextureBufferFactory.CreateTextureBuffer(ref backend, texture);
			return idx++;
		}
	}
}
