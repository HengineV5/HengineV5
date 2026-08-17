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

			var backend = renderContext.CreateBackend();
			materialBuffers.Span[(int)idx] = CreateMaterialBuffer(ref backend, resource);
			return idx++;
		}

		public static PbrMaterialBuffer CreateMaterialBuffer<TBackend>(ref TBackend backend, Graphics.PbrMaterial material) where TBackend : struct, IRenderBackend<TBackend>, allows ref struct
		{
			return new PbrMaterialBuffer()
			{
				albedo = material.albedo,
				albedoMap = TextureBufferFactory.CreateTextureBuffer(ref backend, material.albedoMap),
				metallic = material.metallic,
				metallicMap = TextureBufferFactory.CreateTextureBuffer(ref backend, material.metallicMap),
				roughness = material.roughness,
				roughnessMap = TextureBufferFactory.CreateTextureBuffer(ref backend, material.roughnessMap),
				aoMap = TextureBufferFactory.CreateTextureBuffer(ref backend, material.aoMap),
				normalMap = TextureBufferFactory.CreateTextureBuffer(ref backend, material.normalMap),
				depthMap = TextureBufferFactory.CreateTextureBuffer(ref backend, material.depthMap),
			};
		}
	}
}
