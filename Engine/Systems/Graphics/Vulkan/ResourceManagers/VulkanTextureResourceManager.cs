using EnCS;
using EnCS.Attributes;
using Silk.NET.Vulkan;
using SixLabors.ImageSharp.PixelFormats;
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
			textureBuffers.Span[(int)idx] = CreateTextureBuffer(context, texture);
			return idx++;
		}

		// TODO: Tmp, make private
		public static VkTextureBuffer CreateTextureBuffer(VkContext context, Graphics.ETexture texture)
		{
            VkTextureBuffer textureBuffer = new VkTextureBuffer();
            textureBuffer.texture = VulkanHelper.CreateImage(context, new((uint)texture.data.Width, (uint)texture.data.Height, 1), ImageType.Type2D, Format.R8G8B8A8Srgb, ImageTiling.Optimal, ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit, ImageCreateFlags.None, 1, 1);
            textureBuffer.textureMemory = VulkanHelper.CreateMemory(context, textureBuffer.texture, MemoryPropertyFlags.DeviceLocalBit);
            textureBuffer.textureImageView = VulkanHelper.CreateImageView(context, textureBuffer.texture, ImageViewType.Type2D, Format.R8G8B8A8Srgb, ImageAspectFlags.ColorBit, 1);

            CopyBufferToDevice(context, textureBuffer, [texture.data], Format.R8G8B8A8Srgb);

            return textureBuffer;
		}

        public static VkTextureBuffer CreateCubeTextureBuffer(VkContext context, Graphics.ECubemapHdr texture)
        {
            VkTextureBuffer textureBuffer = new VkTextureBuffer();
            textureBuffer.texture = VulkanHelper.CreateImage(context, new((uint)texture.front.Width, (uint)texture.front.Height, 1), ImageType.Type2D, Format.R16G16B16A16Unorm, ImageTiling.Optimal, ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit, ImageCreateFlags.CreateCubeCompatibleBit, 6, 1);
            textureBuffer.textureMemory = VulkanHelper.CreateMemory(context, textureBuffer.texture, MemoryPropertyFlags.DeviceLocalBit);
            textureBuffer.textureImageView = VulkanHelper.CreateImageView(context, textureBuffer.texture, ImageViewType.TypeCube, Format.R16G16B16A16Unorm, ImageAspectFlags.ColorBit, 1);

            CopyBufferToDevice(context, textureBuffer, [texture.right, texture.left, texture.top, texture.bottom, texture.front, texture.back], Format.R16G16B16A16Unorm);

            return textureBuffer;
        }

        public static VkTextureBuffer CreateCubeTextureBuffer(VkContext context, Graphics.ETextureHdr texture, Format format)
        {
            // Format.R16G16B16A16Unorm

            VkTextureBuffer textureBuffer = new VkTextureBuffer();
            textureBuffer.texture = VulkanHelper.CreateImage(context, new((uint)(texture.data.Width / 4), (uint)(texture.data.Height / 3), 1), ImageType.Type2D, format, ImageTiling.Optimal, ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit, ImageCreateFlags.CreateCubeCompatibleBit, 6, 1);
            textureBuffer.textureMemory = VulkanHelper.CreateMemory(context, textureBuffer.texture, MemoryPropertyFlags.DeviceLocalBit);
            textureBuffer.textureImageView = VulkanHelper.CreateImageView(context, textureBuffer.texture, ImageViewType.TypeCube, format, ImageAspectFlags.ColorBit, 1);

            CopyCubemapBufferToDevice(context, textureBuffer, texture.data, format);

            return textureBuffer;
        }

        public static VkTextureBuffer CreateMipCubeTextureBuffer(VkContext context, Graphics.ETextureHdr texture, uint mipLevels, Format format)
        {
            // Format.R16G16B16A16Unorm

            int imgWidth = texture.data.Width;
            int imgHeight = texture.data.Height;

            if (mipLevels > 1)
                imgWidth = (int)(imgWidth * (2f / 3f));

            VkTextureBuffer textureBuffer = new VkTextureBuffer();
            textureBuffer.texture = VulkanHelper.CreateImage(context, new((uint)(imgWidth / 4), (uint)(imgHeight / 3), 1), ImageType.Type2D, format, ImageTiling.Optimal, ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit, ImageCreateFlags.CreateCubeCompatibleBit, 6, mipLevels);
            textureBuffer.textureMemory = VulkanHelper.CreateMemory(context, textureBuffer.texture, MemoryPropertyFlags.DeviceLocalBit);
            textureBuffer.textureImageView = VulkanHelper.CreateImageView(context, textureBuffer.texture, ImageViewType.TypeCube, format, ImageAspectFlags.ColorBit, mipLevels);

            CopyMipCubemapBufferToDevice(context, textureBuffer, texture.data, format, mipLevels);

            return textureBuffer;
        }

        public static VkTextureBuffer CreateHdrTextureBuffer(VkContext context, Graphics.ETextureHdr texture)
        {
            VkTextureBuffer textureBuffer = new VkTextureBuffer();
            textureBuffer.texture = VulkanHelper.CreateImage(context, new((uint)texture.data.Width, (uint)texture.data.Height, 1), ImageType.Type2D, Format.R16G16B16A16Unorm, ImageTiling.Optimal, ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit, ImageCreateFlags.None, 1, 1);
            textureBuffer.textureMemory = VulkanHelper.CreateMemory(context, textureBuffer.texture, MemoryPropertyFlags.DeviceLocalBit);
            textureBuffer.textureImageView = VulkanHelper.CreateImageView(context, textureBuffer.texture, ImageViewType.Type2D, Format.R16G16B16A16Unorm, ImageAspectFlags.ColorBit, 1);

            CopyBufferToDevice(context, textureBuffer, [texture.data], Format.R16G16B16A16Unorm);

            return textureBuffer;
        }

        static void CopyBufferToDevice<TPixel>(VkContext context, VkTextureBuffer textureBuffer, Span<SixLabors.ImageSharp.Image<TPixel>> imgs, Format format) where TPixel : unmanaged, IPixel<TPixel>
		{
            // TODO: Move command pool to context, bad to create for each mesh creating call
            uint graphicsQueueFamily = VulkanHelper.GetGraphicsQueueFamily(context);
            Queue graphicsQueue = VulkanHelper.GetQueue(context, graphicsQueueFamily);
            CommandPool commandPool = VulkanHelper.CreateCommandPool(context, graphicsQueueFamily);

            int imageSize = imgs[0].Width * imgs[0].Height * imgs[0].PixelType.BitsPerPixel / 8;
            using var buff = MemoryPool<byte>.Shared.Rent(imageSize);

            Buffer stagingBuffer = VulkanHelper.CreateBuffer<byte>(context, BufferUsageFlags.TransferSrcBit, (uint)imageSize);
            DeviceMemory stagingBufferMemory = VulkanHelper.CreateBufferMemory(context, stagingBuffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

            VulkanHelper.TransitionImageLayout(context, commandPool, graphicsQueue, textureBuffer.texture, format, ImageLayout.Undefined, ImageLayout.TransferDstOptimal, (uint)imgs.Length, 1);
            for (int i = 0; i < (uint)imgs.Length; i++)
            {
                imgs[i].CopyPixelDataTo(buff.Memory.Span);

                VulkanHelper.CopyToBuffer(context, stagingBuffer, stagingBufferMemory, buff.Memory.Span);
                VulkanHelper.CopyBufferToImage(context, commandPool, graphicsQueue, stagingBuffer, textureBuffer.texture, (uint)imgs[i].Width, (uint)imgs[i].Height, (uint)i, 0);
            }
            VulkanHelper.TransitionImageLayout(context, commandPool, graphicsQueue, textureBuffer.texture, format, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal, (uint)imgs.Length, 1);

            unsafe
            {
                context.vk.DestroyBuffer(context.device, stagingBuffer, null);
                context.vk.FreeMemory(context.device, stagingBufferMemory, null);
                context.vk.DestroyCommandPool(context.device, commandPool, null);
            }
        }

        static void CopyCubemapBufferToDevice<TPixel>(VkContext context, VkTextureBuffer textureBuffer, SixLabors.ImageSharp.Image<TPixel> img, Format format) where TPixel : unmanaged, IPixel<TPixel>
        {
            // TODO: Move command pool to context, bad to create for each mesh creating call
            uint graphicsQueueFamily = VulkanHelper.GetGraphicsQueueFamily(context);
            Queue graphicsQueue = VulkanHelper.GetQueue(context, graphicsQueueFamily);
            CommandPool commandPool = VulkanHelper.CreateCommandPool(context, graphicsQueueFamily);

            uint sideWidth = (uint)img.Width / 4u;
            uint sideHeight = (uint)img.Height / 3u;

            int bytesPerPixel = img.PixelType.BitsPerPixel / 8;
            int imageSize = img.Width * img.Height * bytesPerPixel;
            int rowLength = img.Width;
            using var buff = MemoryPool<byte>.Shared.Rent(imageSize);

            Buffer stagingBuffer = VulkanHelper.CreateBuffer<byte>(context, BufferUsageFlags.TransferSrcBit, (uint)imageSize);
            DeviceMemory stagingBufferMemory = VulkanHelper.CreateBufferMemory(context, stagingBuffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

            img.CopyPixelDataTo(buff.Memory.Span);
            VulkanHelper.CopyToBuffer(context, stagingBuffer, stagingBufferMemory, buff.Memory.Span);

            VulkanHelper.TransitionImageLayout(context, commandPool, graphicsQueue, textureBuffer.texture, format, ImageLayout.Undefined, ImageLayout.TransferDstOptimal, 6, 1);
            for (int i = 0; i < 6; i++)
            {
                VulkanHelper.CopyBufferToImage(context, commandPool, graphicsQueue, stagingBuffer, textureBuffer.texture, sideWidth, sideHeight, (uint)i, 0, GetSideOffset(i, sideWidth, sideHeight, rowLength) * (ulong)bytesPerPixel, (uint)rowLength);
            }
            VulkanHelper.TransitionImageLayout(context, commandPool, graphicsQueue, textureBuffer.texture, format, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal, 6, 1);

            unsafe
            {
                context.vk.DestroyBuffer(context.device, stagingBuffer, null);
                context.vk.FreeMemory(context.device, stagingBufferMemory, null);
                context.vk.DestroyCommandPool(context.device, commandPool, null);
            }
        }

        static void CopyMipCubemapBufferToDevice<TPixel>(VkContext context, VkTextureBuffer textureBuffer, SixLabors.ImageSharp.Image<TPixel> img, Format format, uint mipLevels) where TPixel : unmanaged, IPixel<TPixel>
        {
            // TODO: Move command pool to context, bad to create for each mesh creating call
            uint graphicsQueueFamily = VulkanHelper.GetGraphicsQueueFamily(context);
            Queue graphicsQueue = VulkanHelper.GetQueue(context, graphicsQueueFamily);
            CommandPool commandPool = VulkanHelper.CreateCommandPool(context, graphicsQueueFamily);

            int imgWidth = img.Width;
            int imgHeight = img.Height;

            if (mipLevels > 1)
                imgWidth = (int)(imgWidth * (2f / 3f));

            uint sideWidth = (uint)imgWidth / 4u;
            uint sideHeight = (uint)imgHeight / 3u;

            int bytesPerPixel = img.PixelType.BitsPerPixel / 8;
            int imageSize = img.Width * img.Height * bytesPerPixel;
            using var buff = MemoryPool<byte>.Shared.Rent(imageSize);

            Buffer stagingBuffer = VulkanHelper.CreateBuffer<byte>(context, BufferUsageFlags.TransferSrcBit, (uint)imageSize);
            DeviceMemory stagingBufferMemory = VulkanHelper.CreateBufferMemory(context, stagingBuffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

            img.CopyPixelDataTo(buff.Memory.Span);
            VulkanHelper.CopyToBuffer(context, stagingBuffer, stagingBufferMemory, buff.Memory.Span);

            VulkanHelper.TransitionImageLayout(context, commandPool, graphicsQueue, textureBuffer.texture, format, ImageLayout.Undefined, ImageLayout.TransferDstOptimal, 6, mipLevels);
            for (uint mipLevel = 0; mipLevel < mipLevels; mipLevel++)
            {
                float mipFactor = MathF.Pow(0.5f, mipLevel);

                uint mipWidth = (uint)(sideWidth * mipFactor);
                uint mipHeight = (uint)(sideHeight * mipFactor);

                int mipStartX = mipLevel == 0 ? 0 : imgWidth;
                int mipStartY = mipLevel == 0 ? 0 : (int)(imgHeight * mipFactor);
                uint mipOffset = (uint)(mipStartX + mipStartY * img.Width);

                for (int side = 0; side < 6; side++)
                {
                    VulkanHelper.CopyBufferToImage(context, commandPool, graphicsQueue, stagingBuffer, textureBuffer.texture, mipWidth, mipHeight, (uint)side, mipLevel, (mipOffset + GetSideOffset(side, mipWidth, mipHeight, img.Width)) * (ulong)bytesPerPixel, (uint)img.Width);
                }
            }
            VulkanHelper.TransitionImageLayout(context, commandPool, graphicsQueue, textureBuffer.texture, format, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal, 6, mipLevels);

            unsafe
            {
                context.vk.DestroyBuffer(context.device, stagingBuffer, null);
                context.vk.FreeMemory(context.device, stagingBufferMemory, null);
                context.vk.DestroyCommandPool(context.device, commandPool, null);
            }
        }

        static ulong GetSideOffset(int side, uint sideWidth, uint sideHeight, int rowLength)
        {
            int offsetX;
            int offsetY;
            switch (side)
            {
                case 3: // 0
                    offsetX = 1;
                    offsetY = 2;
                    break;
                case 1: // 1
                    offsetX = 0;
                    offsetY = 1;
                    break;
                case 4: // 2
                    offsetX = 1;
                    offsetY = 1;
                    break;
                case 2: // 3
                    offsetX = 1;
                    offsetY = 0;
                    break;
                case 0: // 4
                    offsetX = 2;
                    offsetY = 1;
                    break;
                case 5: // 5
                    offsetX = 3;
                    offsetY = 1;
                    break;
                default:
                    throw new Exception("Unsupported side.");
            }

            return (ulong)(offsetY * sideHeight * rowLength + offsetX * sideWidth);
        }
    }
}
