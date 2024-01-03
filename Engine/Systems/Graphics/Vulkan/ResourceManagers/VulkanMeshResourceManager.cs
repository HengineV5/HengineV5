using EnCS;
using EnCS.Attributes;
using Engine.Components.Graphics;
using Engine.Graphics;
using Silk.NET.OpenAL;
using Silk.NET.Vulkan;
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
		Memory<Graphics.Mesh> meshes = new Graphics.Mesh[32];
		Memory<Graphics.VkMeshBuffer> meshBuffers = new Graphics.VkMeshBuffer[32];

		Dictionary<string, uint> meshCache = new Dictionary<string, uint>();

		VkContext context;

		public VulkanMeshResourceManager(VkContext context)
		{
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

            meshCache.Add(mesh.name, idx);
			meshes.Span[(int)idx] = mesh;
			meshBuffers.Span[(int)idx] = CreateMeshBuffer(context, mesh);
			return idx++;
		}

		static VkMeshBuffer CreateMeshBuffer(VkContext context, Graphics.Mesh mesh)
		{
			// Convert mesh data to correct format
			uint indiciesLength = (uint)mesh.indicies.Length;
			Memory<Vertex> verticies = mesh.verticies;
			Memory<ushort> indicies = new ushort[mesh.indicies.Length];
			for (int i = 0; i < mesh.indicies.Length; i++)
			{
				indicies.Span[i] = (ushort)mesh.indicies[i];
			}

			// TODO: Move command pool to context, bad to create for each mesh creating call
			uint graphicsQueueFamily = VulkanHelper.GetGraphicsQueueFamily(context);
			Queue graphicsQueue = VulkanHelper.GetQueue(context, graphicsQueueFamily);
			CommandPool commandPool = VulkanHelper.CreateCommandPool(context, graphicsQueueFamily);

			VkMeshBuffer meshBuffer = new VkMeshBuffer();
			meshBuffer.indicies = (uint)mesh.indicies.Length;

			meshBuffer.vertexBuffer = VulkanHelper.CreateBuffer<Vertex>(context, BufferUsageFlags.TransferDstBit | BufferUsageFlags.VertexBufferBit, (uint)verticies.Length);
			meshBuffer.vertexBufferMemory = VulkanHelper.CreateBufferMemory(context, meshBuffer.vertexBuffer, MemoryPropertyFlags.DeviceLocalBit);
			meshBuffer.indexBuffer = VulkanHelper.CreateBuffer<ushort>(context, BufferUsageFlags.TransferDstBit | BufferUsageFlags.IndexBufferBit, (uint)indicies.Length);
			meshBuffer.indexBufferMemory = VulkanHelper.CreateBufferMemory(context, meshBuffer.indexBuffer, MemoryPropertyFlags.DeviceLocalBit);

			Buffer stagingBuffer = VulkanHelper.CreateBuffer<Vertex>(context, BufferUsageFlags.TransferSrcBit, (uint)verticies.Length);
			DeviceMemory stagingBufferMemory = VulkanHelper.CreateBufferMemory(context, stagingBuffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

			VulkanHelper.MapBufferMemory(context, stagingBuffer, stagingBufferMemory, verticies.Span);
			VulkanHelper.CopyBuffer(context, commandPool, graphicsQueue, stagingBuffer, meshBuffer.vertexBuffer, (uint)verticies.Span.Length * Vertex.SizeInBytes);

			VulkanHelper.MapBufferMemory(context, stagingBuffer, stagingBufferMemory, indicies.Span);
			VulkanHelper.CopyBuffer(context, commandPool, graphicsQueue, stagingBuffer, meshBuffer.indexBuffer, (uint)indicies.Span.Length * sizeof(ushort));

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
