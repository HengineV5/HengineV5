using EnCS;
using EnCS.Attributes;
using Microsoft.Extensions.Logging;

using RenderLib;

namespace Engine.Graphics
{
	public struct SkyboxBuffer
	{
		public TextureBuffer skybox;
		public TextureBuffer irradiance;
		public TextureBuffer specular;
	}

	[ResourceManager]
	public partial class SkyboxResourceManager : IResourceManager<Skybox, SkyboxBuffer>
	{
		uint idx = 0;
		Memory<Graphics.SkyboxBuffer> skyboxBuffers = new Graphics.SkyboxBuffer[32];

		Dictionary<string, uint> skyboxCache = new Dictionary<string, uint>();

		GraphicsContext renderContext;
		ILogger logger;

		public SkyboxResourceManager(ILoggerFactory factory, GraphicsContext renderContext)
		{
			this.logger = factory.CreateLogger<SkyboxResourceManager>();
			this.renderContext = renderContext;
		}

		public ref SkyboxBuffer Get(uint id)
		{
			return ref skyboxBuffers.Span[(int)id];
		}

		public uint Store(in Graphics.Skybox resource)
		{
			if (skyboxCache.TryGetValue(resource.name, out uint id))
				return id;

			logger.LogResourceManagerStore(resource.name);

			skyboxCache.Add(resource.name, idx);

			var storage = renderContext.Storage;
			skyboxBuffers.Span[(int)idx] = CreateSkyboxBuffer(ref storage, resource);

			return idx++;
		}

		public static SkyboxBuffer CreateSkyboxBuffer<TStorage>(ref TStorage storage, Graphics.Skybox skybox) where TStorage : struct, IRenderBackend<TStorage>, allows ref struct
		{
			return new SkyboxBuffer()
			{
				skybox = TextureBufferFactory.CreateCrossCubeTextureBuffer(ref storage, skybox.skybox, TextureFormat.R16G16B16A16Unorm),
				irradiance = TextureBufferFactory.CreateCrossCubeTextureBuffer(ref storage, skybox.irradiance, TextureFormat.R16G16B16A16Unorm),
				specular = TextureBufferFactory.CreateMipCrossCubeTextureBuffer(ref storage, skybox.specular, 5, TextureFormat.R16G16B16A16Unorm)
			};
		}
	}
}
