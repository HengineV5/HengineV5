using EnCS;
using EnCS.Attributes;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Runtime.InteropServices;

using RenderLib;

namespace Engine.Graphics
{
	public struct MeshBuffer
	{
		public GpuBuffer vertexBuffer;

		public uint indicies;
		public GpuBuffer indexBuffer;
	}

	public static class MeshBufferFactory
	{
		public static MeshBuffer CreateMeshBuffer<TBackend>(ref TBackend backend, Mesh mesh) where TBackend : struct, IRenderBackend<TBackend>, allows ref struct
		{
			using var indicies = MemoryPool<ushort>.Shared.Rent(mesh.indicies.Length);

			for (int i = 0; i < mesh.indicies.Length; i++)
			{
				indicies.Memory.Span[i] = (ushort)mesh.indicies[i];
			}

			return CreateBuffer(ref backend, mesh.verticies.AsSpan(), indicies.Memory.Span.Slice(0, mesh.indicies.Length));
		}

		public static MeshBuffer CreateGizmoBuffer<TBackend>(ref TBackend backend, Mesh mesh) where TBackend : struct, IRenderBackend<TBackend>, allows ref struct
		{
			using var verticies = MemoryPool<GizmoVertex>.Shared.Rent(mesh.verticies.Length);
			using var indicies = MemoryPool<ushort>.Shared.Rent(mesh.indicies.Length);

			for (int i = 0; i < mesh.indicies.Length; i++)
			{
				indicies.Memory.Span[i] = (ushort)mesh.indicies[i];
			}

			for (int i = 0; i < mesh.verticies.Length; i++)
			{
				verticies.Memory.Span[i] = new(mesh.verticies[i].position, mesh.verticies[i].normal);
			}

			return CreateBuffer(ref backend, verticies.Memory.Span.Slice(0, mesh.verticies.Length), indicies.Memory.Span.Slice(0, mesh.indicies.Length));
		}

		public static MeshBuffer CreateBuffer<TBackend, TVertex>(ref TBackend backend, scoped Span<TVertex> vertices, scoped Span<ushort> indicies)
			where TBackend : struct, IRenderBackend<TBackend>, allows ref struct
			where TVertex : unmanaged, IVertex
		{
			MeshBuffer meshBuffer = new MeshBuffer();

			meshBuffer.indicies = (uint)indicies.Length;
			meshBuffer.vertexBuffer = TBackend.CreateVertexBuffer(ref backend, MemoryMarshal.AsBytes(vertices));
			meshBuffer.indexBuffer = TBackend.CreateIndexBuffer(ref backend, MemoryMarshal.AsBytes(indicies));

			return meshBuffer;
		}
	}

	[ResourceManager]
	public partial class MeshResourceManager : IResourceManager<Mesh, MeshBuffer>
	{
		uint idx = 0;
		Memory<Graphics.MeshBuffer> meshBuffers = new Graphics.MeshBuffer[32];

		Dictionary<string, uint> meshCache = new Dictionary<string, uint>();

		GraphicsContext renderContext;
		ILogger logger;

		public MeshResourceManager(ILoggerFactory factory, GraphicsContext renderContext)
		{
			this.logger = factory.CreateLogger<MeshResourceManager>();
			this.renderContext = renderContext;
		}

		public ref Graphics.MeshBuffer Get(uint id)
		{
			return ref meshBuffers.Span[(int)id];
		}

		public uint Store(in Graphics.Mesh mesh)
		{
			if (meshCache.TryGetValue(mesh.name, out uint id))
				return id;

			logger.LogResourceManagerStore(mesh.name);

			meshCache.Add(mesh.name, idx);
			var backend = renderContext.CreateBackend();
			meshBuffers.Span[(int)idx] = MeshBufferFactory.CreateMeshBuffer(ref backend, mesh);
			return idx++;
		}
	}
}
