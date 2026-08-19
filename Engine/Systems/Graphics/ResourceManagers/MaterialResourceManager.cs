using EnCS;
using EnCS.Attributes;
using Microsoft.Extensions.Logging;

using RenderLib;

namespace Engine.Graphics
{
	public struct PbrMaterialBuffer
	{
		public Vector3f albedo;
		public TextureBuffer albedoMap;
		public float metallic;
		public TextureBuffer metallicMap;
		public float roughness;
		public TextureBuffer roughnessMap;
		public TextureBuffer aoMap;
		public TextureBuffer normalMap;
		public TextureBuffer depthMap;
	}

	[ResourceManager]
	public partial class MaterialResourceManager : IResourceManager<PbrMaterial, PbrMaterialBuffer>
	{
		uint idx = 0;
		Memory<Graphics.PbrMaterialBuffer> materialBuffers = new Graphics.PbrMaterialBuffer[256];

		Dictionary<string, uint> materialCache = new Dictionary<string, uint>();

		GraphicsContext renderContext;
		ILogger logger;

		public MaterialResourceManager(ILoggerFactory factory, GraphicsContext renderContext)
		{
			this.logger = factory.CreateLogger<MaterialResourceManager>();
			this.renderContext = renderContext;
		}

		public ref PbrMaterialBuffer Get(uint id)
		{
			return ref materialBuffers.Span[(int)id];
		}

		public uint Store(in Graphics.PbrMaterial resource)
		{
			if (materialCache.TryGetValue(resource.name, out uint id))
				return id;

			logger.LogResourceManagerStore(resource.name);

			materialCache.Add(resource.name, idx);

			var storage = renderContext.Storage;
			materialBuffers.Span[(int)idx] = CreateMaterialBuffer(ref storage, resource);
			return idx++;
		}

		public static PbrMaterialBuffer CreateMaterialBuffer<TStorage>(ref TStorage storage, Graphics.PbrMaterial material) where TStorage : struct, IRenderBackend<TStorage>, allows ref struct
		{
			return new PbrMaterialBuffer()
			{
				albedo = material.albedo,
				albedoMap = TextureBufferFactory.CreateTextureBuffer(ref storage, material.albedoMap),
				metallic = material.metallic,
				metallicMap = TextureBufferFactory.CreateTextureBuffer(ref storage, material.metallicMap),
				roughness = material.roughness,
				roughnessMap = TextureBufferFactory.CreateTextureBuffer(ref storage, material.roughnessMap),
				aoMap = TextureBufferFactory.CreateTextureBuffer(ref storage, material.aoMap),
				normalMap = TextureBufferFactory.CreateTextureBuffer(ref storage, material.normalMap),
				depthMap = TextureBufferFactory.CreateTextureBuffer(ref storage, material.depthMap),
			};
		}
	}
}
