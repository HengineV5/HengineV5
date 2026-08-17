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

			var backend = renderContext.CreateBackend();
			skyboxBuffers.Span[(int)idx] = CreateSkyboxBuffer(ref backend, resource);

			return idx++;
		}

		public static SkyboxBuffer CreateSkyboxBuffer<TBackend>(ref TBackend backend, Graphics.Skybox skybox) where TBackend : struct, IRenderBackend<TBackend>, allows ref struct
		{
			return new SkyboxBuffer()
			{
				skybox = TextureBufferFactory.CreateCrossCubeTextureBuffer(ref backend, skybox.skybox, TextureFormat.R16G16B16A16Unorm),
				irradiance = TextureBufferFactory.CreateCrossCubeTextureBuffer(ref backend, skybox.irradiance, TextureFormat.R16G16B16A16Unorm),
				specular = TextureBufferFactory.CreateMipCrossCubeTextureBuffer(ref backend, skybox.specular, 5, TextureFormat.R16G16B16A16Unorm)
			};
		}
	}
}
