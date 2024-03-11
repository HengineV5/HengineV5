using Silk.NET.Vulkan;
using EnCS;
using SixLabors.ImageSharp.PixelFormats;
using Image = Silk.NET.Vulkan.Image;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Engine
{
    /*
    public struct TextureRenderTargetManager<TDescriptorSet, TPixel> : IRenderTargetManager<TextureRenderTargetManager<TDescriptorSet, TPixel>, TDescriptorSet, DefaultRenderPassInfo, DefaultPipelineInfo>
        where TDescriptorSet : struct, IDescriptorSet<TDescriptorSet>
        where TPixel : unmanaged, IPixel<TPixel>
    {
        Action<SixLabors.ImageSharp.Image<TPixel>> imgAction;

        Extent2D extent;
        RenderPass compatibleRenderPass;
        FixedArray2<(Image, Format)> images;
        Framebuffer framebuffer;
        FrameInFlight<TDescriptorSet> frameInFlight;

        public TextureRenderTargetManager(Action<SixLabors.ImageSharp.Image<TPixel>> imgAction, Extent2D extent, RenderPass compatibleRenderPass, FixedArray2<(Image, Format)> images, Framebuffer framebuffer, FrameInFlight<TDescriptorSet> frameInFlight)
        {
            this.imgAction = imgAction;
            this.extent = extent;
            this.compatibleRenderPass = compatibleRenderPass;
            this.images = images;
            this.framebuffer = framebuffer;
            this.frameInFlight = frameInFlight;
        }

        public static RenderTarget<TDescriptorSet> AquireRenderTarget(VkContext context, ref TextureRenderTargetManager<TDescriptorSet, TPixel> self)
        {
            SemaphoreSignalInfo signalInfo = new();
            signalInfo.SType = StructureType.SemaphoreSignalInfo;
            signalInfo.Semaphore = self.frameInFlight.imageAvailable;
            signalInfo.Value = 1;

            //context.vk.SignalSemaphore(context.device, signalInfo);

            return new RenderTarget<TDescriptorSet>()
            {
                frame = self.frameInFlight,
                framebuffer = self.framebuffer,
                imageIndex = 0
            };
        }

        public static DefaultPipelineInfo GetPipelineInfo(VkContext context, ref TextureRenderTargetManager<TDescriptorSet, TPixel> self)
        {
            return new DefaultPipelineInfo(self.extent, self.compatibleRenderPass);
        }

        public static DefaultRenderPassInfo GetRendePassInfo(VkContext context, ref TextureRenderTargetManager<TDescriptorSet, TPixel> self)
        {
            return new DefaultRenderPassInfo(self.images[0].Item2, self.images[1].Item2);
        }

        public static Rect2D GetRenderArea(ref TextureRenderTargetManager<TDescriptorSet, TPixel> self)
        {
            return new Rect2D(new(), self.extent);
        }

        public static void PresentTarget(VkContext context, ref TextureRenderTargetManager<TDescriptorSet, TPixel> self, ref RenderTarget<TDescriptorSet> renderTarget)
        {
            //context.vk.DeviceWaitIdle(context.device);
            while (true)
            {
                context.vk.GetSemaphoreCounterValue(context.device, renderTarget.frame.imageAvailable, out var value);
                //Console.WriteLine(value);
                if (value >= 1)
                {
                    return;
                }
            }

            // TODO: Move command pool to context, bad to create for each mesh creating call
            uint graphicsQueueFamily = VulkanHelper.GetGraphicsQueueFamily(context);
            Queue graphicsQueue = VulkanHelper.GetQueue(context, graphicsQueueFamily);
            CommandPool commandPool = VulkanHelper.CreateCommandPool(context, graphicsQueueFamily);

            uint imageSize = 4 * self.extent.Width * self.extent.Height;
            Buffer stagingBuffer = VulkanHelper.CreateBuffer<byte>(context, BufferUsageFlags.TransferDstBit, imageSize);
            DeviceMemory stagingBufferMemory = VulkanHelper.CreateBufferMemory(context, stagingBuffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

            // Convert swapchain image to tranfer src
            VulkanHelper.TransitionImageLayout(context, commandPool, graphicsQueue, self.images[0].Item1, self.images[0].Item2, ImageLayout.PresentSrcKhr, ImageLayout.TransferSrcOptimal, 1, 1);

            //VulkanHelper.TransitionImageLayout(context, commandPool, graphicsQueue, self.dstImage, Format.R8G8B8A8Srgb, ImageLayout.ColorAttachmentOptimal, ImageLayout.TransferDstOptimal, 1);
            //VulkanHelper.CopyImage(context, commandPool, graphicsQueue, self.images[0], self.dstImage, new Extent3D(self.extent.Width, self.extent.Height, 1));
            //VulkanHelper.TransitionImageLayout(context, commandPool, graphicsQueue, self.dstImage, Format.R8G8B8A8Srgb, ImageLayout.TransferDstOptimal, ImageLayout.TransferSrcOptimal, 1);

            // Transfer image to staging buffer visible to host
            //VulkanHelper.CopyImageToBuffer(context, commandPool, graphicsQueue, self.dstImage, self.extent.Width, self.extent.Height, stagingBuffer, 0, 1);
            VulkanHelper.CopyImageToBuffer(context, commandPool, graphicsQueue, self.images[0].Item1, self.extent.Width, self.extent.Height, stagingBuffer, 0);

            //VulkanHelper.TransitionImageLayout(context, commandPool, graphicsQueue, self.dstImage, Format.R8G8B8A8Srgb, ImageLayout.TransferSrcOptimal, ImageLayout.ColorAttachmentOptimal, 1);

            // Convert image back to swapchain usable
            VulkanHelper.TransitionImageLayout(context, commandPool, graphicsQueue, self.images[0].Item1, self.images[0].Item2, ImageLayout.TransferSrcOptimal, ImageLayout.PresentSrcKhr, 1, 1);

            var buff = new VulkanBuffer(context, stagingBuffer, stagingBufferMemory);

            using var img = SixLabors.ImageSharp.Image.LoadPixelData<TPixel>(buff.Span, (int)self.extent.Width, (int)self.extent.Height);
            self.imgAction(img);

            buff.Unmap();

            unsafe
            {
                context.vk.DestroyBuffer(context.device, stagingBuffer, null);
                context.vk.FreeMemory(context.device, stagingBufferMemory, null);
                context.vk.DestroyCommandPool(context.device, commandPool, null);
            }
        }

        public static TextureRenderTargetManager<TDescriptorSet, TPixel> Create(Action<SixLabors.ImageSharp.Image<TPixel>> imgAction, VkContext context, Extent2D extent, FixedArray2<Image> images, FixedArray2<ImageView> imageViews, FixedArray2<Format> formats, RenderPass compatibleRenderPass, CommandPool commandPool)
        {
            Framebuffer framebuffer = VulkanHelper.CreateFrameBuffer(context, imageViews, compatibleRenderPass, extent);
            FixedArray2<(Image, Format)> imageFormats = new FixedArray2<(Silk.NET.Vulkan.Image, Format)>();
            imageFormats[0] = (images[0], formats[0]);
            imageFormats[1] = (images[1], formats[1]);

            DescriptorPool descriptorPool = TDescriptorSet.GetPool(context, 16);
            FixedArray16<TDescriptorSet> descriptorSets = new FixedArray16<TDescriptorSet>();

            for (int a = 0; a < 16; a++)
            {
                descriptorSets[a] = TDescriptorSet.Create(context, descriptorPool);
            }

            FrameInFlight<TDescriptorSet> frameInFlight = new FrameInFlight<TDescriptorSet>(
                VulkanHelper.CreateSemaphore(context),
                VulkanHelper.CreateSemaphore(context),
                VulkanHelper.CreateFence(context, FenceCreateFlags.SignaledBit),
                VulkanHelper.CreateCommandBuffer(context, commandPool),
                descriptorSets
            );

            uint graphicsQueueFamily = VulkanHelper.GetGraphicsQueueFamily(context);
            Queue graphicsQueue = VulkanHelper.GetQueue(context, graphicsQueueFamily);

            //VulkanHelper.TransitionImageLayout(context, commandPool, graphicsQueue, dstImage, Format.R8G8B8A8Srgb, ImageLayout.Undefined, ImageLayout.TransferSrcOptimal, 1);
            //VulkanHelper.TransitionImageLayout(context, commandPool, graphicsQueue, dstImage, Format.R8G8B8A8Srgb, ImageLayout.TransferSrcOptimal, ImageLayout.ColorAttachmentOptimal, 1);

            return new TextureRenderTargetManager<TDescriptorSet, TPixel>(imgAction, extent, compatibleRenderPass, imageFormats, framebuffer, frameInFlight);
        }
    }
    */
}
