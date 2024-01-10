using EnCS;
using EnCS.Attributes;
using Silk.NET.Vulkan;
using System.Buffers;
using Buffer = Silk.NET.Vulkan.Buffer;
using Image = Silk.NET.Vulkan.Image;

namespace Engine.Graphics
{
	public struct VkTextureBuffer
	{
		public Image texture;
		public DeviceMemory textureMemory;
		public ImageView textureImageView;
	}

	[ResourceManager]
	public partial class VulkanTextureResourceManager : IResourceManager<ETexture, VkTextureBuffer>
	{
		uint idx = 0;
		Memory<Graphics.ETexture> textures = new Graphics.ETexture[32];
		Memory<Graphics.VkTextureBuffer> textureBuffers = new Graphics.VkTextureBuffer[32];

		Dictionary<string, uint> textureCache = new Dictionary<string, uint>();

		VkContext context;

        public VulkanTextureResourceManager(VkContext context)
        {
			this.context = context;
        }

        public ref Graphics.VkTextureBuffer Get(uint id)
		{
			return ref textureBuffers.Span[(int)id];
		}

		public uint Store(in Graphics.ETexture texture)
		{
			if (textureCache.TryGetValue(texture.name, out uint id))
				return id;

			textureCache.Add(texture.name, idx);
			textures.Span[(int)idx] = texture;
			textureBuffers.Span[(int)idx] = CreateTextureBuffer(context, texture);
			return idx++;
		}

		// TODO: Tmp, make private
		public static VkTextureBuffer CreateTextureBuffer(VkContext context, Graphics.ETexture texture)
		{
            VkTextureBuffer textureBuffer = new VkTextureBuffer();
            textureBuffer.texture = VulkanHelper.CreateImage(context, new((uint)texture.data.Width, (uint)texture.data.Height, 1), ImageType.Type2D, Format.R8G8B8A8Srgb, ImageTiling.Optimal, ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit, ImageCreateFlags.None, 1);
            textureBuffer.textureMemory = VulkanHelper.CreateMemory(context, textureBuffer.texture, MemoryPropertyFlags.DeviceLocalBit);
            textureBuffer.textureImageView = VulkanHelper.CreateImageView(context, textureBuffer.texture, ImageViewType.Type2D, Format.R8G8B8A8Srgb, ImageAspectFlags.ColorBit);

            CopyBufferToDevice(context, textureBuffer, texture.data, false);

            return textureBuffer;
		}

        public static VkTextureBuffer CreateCubeTextureBuffer(VkContext context, Graphics.ETexture texture)
        {
            VkTextureBuffer textureBuffer = new VkTextureBuffer();
            textureBuffer.texture = VulkanHelper.CreateImage(context, new((uint)texture.data.Width, (uint)texture.data.Height, 1), ImageType.Type2D, Format.R8G8B8A8Srgb, ImageTiling.Optimal, ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit, ImageCreateFlags.CreateCubeCompatibleBit, 6);
            textureBuffer.textureMemory = VulkanHelper.CreateMemory(context, textureBuffer.texture, MemoryPropertyFlags.DeviceLocalBit);
            textureBuffer.textureImageView = VulkanHelper.CreateImageView(context, textureBuffer.texture, ImageViewType.TypeCube, Format.R8G8B8A8Srgb, ImageAspectFlags.ColorBit);

            CopyBufferToDevice(context, textureBuffer, texture.data, true);

            return textureBuffer;
        }

        static void CopyBufferToDevice(VkContext context, VkTextureBuffer textureBuffer, Image<Rgba32> img, bool isCubemap)
		{
            // TODO: Move command pool to context, bad to create for each mesh creating call
            uint graphicsQueueFamily = VulkanHelper.GetGraphicsQueueFamily(context);
            Queue graphicsQueue = VulkanHelper.GetQueue(context, graphicsQueueFamily);
            CommandPool commandPool = VulkanHelper.CreateCommandPool(context, graphicsQueueFamily);

            int imageSize = img.Width * img.Height * img.PixelType.BitsPerPixel / 8;
            using var buff = MemoryPool<byte>.Shared.Rent(imageSize);
            img.CopyPixelDataTo(buff.Memory.Span);

            Buffer stagingBuffer = VulkanHelper.CreateBuffer<byte>(context, BufferUsageFlags.TransferSrcBit, (uint)imageSize);
            DeviceMemory stagingBufferMemory = VulkanHelper.CreateBufferMemory(context, stagingBuffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

            VulkanHelper.MapBufferMemory(context, stagingBuffer, stagingBufferMemory, buff.Memory.Span);

            VulkanHelper.TransitionImageLayout(context, commandPool, graphicsQueue, textureBuffer.texture, Format.R8G8B8A8Srgb, ImageLayout.Undefined, ImageLayout.TransferDstOptimal, isCubemap ? 6u : 1u);
            for (int i = 0; i < (isCubemap ? 6 : 1); i++)
            {
                VulkanHelper.CopyBuffer(context, commandPool, graphicsQueue, stagingBuffer, textureBuffer.texture, (uint)img.Width, (uint)img.Height, (uint)i, 1);
            }
            VulkanHelper.TransitionImageLayout(context, commandPool, graphicsQueue, textureBuffer.texture, Format.R8G8B8A8Srgb, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal, isCubemap ? 6u : 1u);

            unsafe
            {
                context.vk.DestroyBuffer(context.device, stagingBuffer, null);
                context.vk.FreeMemory(context.device, stagingBufferMemory, null);
                context.vk.DestroyCommandPool(context.device, commandPool, null);
            }
        }
	}
}
