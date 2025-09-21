using EnCS;
using EnCS.Attributes;
using Microsoft.Extensions.Logging;
using Silk.NET.Vulkan;
using System.Buffers;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Engine.Graphics
{
	public struct VkMeshBuffer
	{
		public Buffer vertexBuffer;
		public DeviceMemory vertexBufferMemory;

		public uint indicies;
		public Buffer indexBuffer;
		public DeviceMemory indexBufferMemory;
	}

	[ResourceManager]
	public partial class VulkanMeshResourceManager : IResourceManager<Mesh, VkMeshBuffer>
	{
		uint idx = 0;
		Memory<Graphics.VkMeshBuffer> meshBuffers = new Graphics.VkMeshBuffer[32];

		Dictionary<string, uint> meshCache = new Dictionary<string, uint>();

		VkContext context;
		ILogger logger;

		public VulkanMeshResourceManager(ILoggerFactory factory, VkContext context)
		{
			this.logger = factory.CreateLogger<VulkanMeshResourceManager>();
			this.context = context;
		}

		public ref Graphics.VkMeshBuffer Get(uint id)
		{
			return ref meshBuffers.Span[(int)id];
		}

		public uint Store(in Graphics.Mesh mesh)
		{
			if (meshCache.TryGetValue(mesh.name, out uint id))
				return id;

			logger.LogResourceManagerStore(mesh.name);

			meshCache.Add(mesh.name, idx);
			meshBuffers.Span[(int)idx] = CreateMeshBuffer(context, mesh);
			return idx++;
		}

		// TODO: Make private
		public static VkMeshBuffer CreateMeshBuffer(VkContext context, Graphics.Mesh mesh)
		{
			// Convert mesh data to correct format
			uint indiciesLength = (uint)mesh.indicies.Length;
			var verticies = mesh.verticies;
			using var indicies = MemoryPool<ushort>.Shared.Rent(mesh.indicies.Length);

			for (int i = 0; i < mesh.indicies.Length; i++)
			{
				indicies.Memory.Span[i] = (ushort)mesh.indicies[i];
			}

			return CreateBuffer(context, verticies.AsSpan(), indicies.Memory.Span.Slice(0, mesh.indicies.Length));
		}

		public static VkMeshBuffer CreateGizmoBuffer(VkContext context, Graphics.Mesh mesh)
		{
			// Convert mesh data to correct format
			uint indiciesLength = (uint)mesh.indicies.Length;
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

			return CreateBuffer(context, verticies.Memory.Span.Slice(0, mesh.verticies.Length), indicies.Memory.Span.Slice(0, mesh.indicies.Length));
		}

		public static VkMeshBuffer CreateBuffer<TVertex>(VkContext context, scoped Span<TVertex> vertices, scoped Span<ushort> indicies) where TVertex : unmanaged, IVertex
		{
			// TODO: Move command pool to context, bad to create for each mesh creating call
			uint graphicsQueueFamily = VulkanHelper.GetGraphicsQueueFamily(context);
			Queue graphicsQueue = VulkanHelper.GetQueue(context, graphicsQueueFamily);
			CommandPool commandPool = VulkanHelper.CreateCommandPool(context, graphicsQueueFamily);

			VkMeshBuffer meshBuffer = new VkMeshBuffer();
			meshBuffer.indicies = (uint)indicies.Length;

			meshBuffer.vertexBuffer = VulkanHelper.CreateBuffer<Vertex>(context, BufferUsageFlags.TransferDstBit | BufferUsageFlags.VertexBufferBit, (uint)vertices.Length);
			meshBuffer.vertexBufferMemory = VulkanHelper.CreateBufferMemory(context, meshBuffer.vertexBuffer, MemoryPropertyFlags.DeviceLocalBit);
			meshBuffer.indexBuffer = VulkanHelper.CreateBuffer<ushort>(context, BufferUsageFlags.TransferDstBit | BufferUsageFlags.IndexBufferBit, (uint)indicies.Length);
			meshBuffer.indexBufferMemory = VulkanHelper.CreateBufferMemory(context, meshBuffer.indexBuffer, MemoryPropertyFlags.DeviceLocalBit);

			Buffer stagingBuffer = VulkanHelper.CreateBuffer<Vertex>(context, BufferUsageFlags.TransferSrcBit, (uint)vertices.Length);
			DeviceMemory stagingBufferMemory = VulkanHelper.CreateBufferMemory(context, stagingBuffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

			VulkanHelper.CopyToBuffer(context, stagingBuffer, stagingBufferMemory, vertices);
			VulkanHelper.CopyBuffer(context, commandPool, graphicsQueue, stagingBuffer, meshBuffer.vertexBuffer, (uint)vertices.Length * TVertex.SizeInBytes);

			VulkanHelper.CopyToBuffer(context, stagingBuffer, stagingBufferMemory, indicies);
			VulkanHelper.CopyBuffer(context, commandPool, graphicsQueue, stagingBuffer, meshBuffer.indexBuffer, (uint)indicies.Length * sizeof(ushort));

			unsafe
			{
				context.vk.DestroyBuffer(context.device, stagingBuffer, null);
				context.vk.FreeMemory(context.device, stagingBufferMemory, null);
				context.vk.DestroyCommandPool(context.device, commandPool, null);
			}

			return meshBuffer;
		}
	}
}
