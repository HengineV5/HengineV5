using EnCS;
using Silk.NET.OpenAL;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;
using Image = Silk.NET.Vulkan.Image;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Engine
{
    public struct FrameInFlight
    {
        public Semaphore imageAvailable;
        public Semaphore renderFinished;
        public Fence inFlight;
        public CommandBuffer commandBuffer;

        public FrameInFlight(Semaphore imageAvailable, Semaphore renderFinished, Fence inFlight, CommandBuffer commandBuffer)
        {
            this.imageAvailable = imageAvailable;
            this.renderFinished = renderFinished;
            this.inFlight = inFlight;
            this.commandBuffer = commandBuffer;
        }
    }

    public struct RenderTarget
    {
        public FrameInFlight frame;
        public Framebuffer framebuffer;
        public uint imageIndex; // TODO: Remove
    }

    public struct RenderPipeline<TRenderTargetManager, TRenderPassInfo, TPipelineInfo, TDescriptorContainer, TPipelineContainer, TPipelineEnum, TRenderPassContainer, TRenderPassEnum>
		where TRenderTargetManager : struct, IRenderTargetManager<TRenderTargetManager, TRenderPassInfo, TPipelineInfo>
		where TRenderPassInfo : struct
		where TPipelineInfo : struct
		where TDescriptorContainer : struct, IDescriptorContainer<TDescriptorContainer, TPipelineEnum>
		where TPipelineContainer : IPipelineContainer<TPipelineContainer, TPipelineInfo, TPipelineEnum>
		where TRenderPassContainer : IRenderPassContainer<TRenderPassContainer, TRenderPassInfo, TRenderPassEnum>
		where TPipelineEnum : Enum
		where TRenderPassEnum : Enum
	{
        TRenderTargetManager renderTargetManager;

        CommandPool commandPool;
		Queue graphicsQueue;

        TDescriptorContainer descriptorSets;
		TPipelineContainer pipelines;
        TRenderPassContainer renderPasses;

		ClearColorValue clearColor;
		RenderTarget renderTarget;

        public RenderPipeline(Queue graphicsQueue, CommandPool commandPool, TRenderTargetManager renderTargetManager, TDescriptorContainer descriptorSets, TPipelineContainer layers, TRenderPassContainer renderPasses)
        {
			this.graphicsQueue = graphicsQueue;
            this.commandPool = commandPool;
            this.renderTargetManager = renderTargetManager;
            this.descriptorSets = descriptorSets;
            this.pipelines = layers;
            this.renderPasses = renderPasses;

            System.Drawing.Color color = System.Drawing.Color.CornflowerBlue;
            this.clearColor = new() { Float32_0 = color.R / 255f, Float32_1 = color.G / 255f, Float32_2 = color.B / 255f, Float32_3 = color.A / 255f };
        }

		public bool StartRender(VkContext context)
		{
			renderTarget = TRenderTargetManager.AquireRenderTarget(context, ref renderTargetManager);

            return true;
        }

		public void StartRenderPass(VkContext context, in TRenderPassEnum renderPass, in TPipelineEnum pipeline)
		{
            var renderArea = TRenderTargetManager.GetRenderArea(ref renderTargetManager);
			var vkRenderPass = TRenderPassContainer.Get(renderPass, ref renderPasses);

            VulkanHelper.WaitForFence(context, renderTarget.frame.inFlight);
            context.vk.ResetCommandBuffer(renderTarget.frame.commandBuffer, CommandBufferResetFlags.ReleaseResourcesBit);

            BeginRenderCommand(context, renderTarget.frame.commandBuffer, vkRenderPass, renderTarget.framebuffer, clearColor, renderArea);
            RenderSetViewportAndScissor(context, renderTarget.frame.commandBuffer, renderArea);

            context.vk.CmdBindPipeline(renderTarget.frame.commandBuffer, PipelineBindPoint.Graphics, TPipelineContainer.Get(pipeline, ref pipelines));
        }

        public unsafe void Render(VkContext context, in TPipelineEnum pipeline, in Buffer vertexBuffer, in Buffer indexBuffer, uint indicies, int idx)
        {
            BindDescriptorSets(context, pipeline, idx);

            context.vk.CmdBindVertexBuffers(renderTarget.frame.commandBuffer, 0, [vertexBuffer], [0]);
            context.vk.CmdBindIndexBuffer(renderTarget.frame.commandBuffer, indexBuffer, 0, IndexType.Uint16);

            context.vk.CmdDrawIndexed(renderTarget.frame.commandBuffer, indicies, 1, 0, 0, 0);
        }

		public unsafe void RenderWithoutUbo(VkContext context, in TPipelineEnum pipeline, in Buffer vertexBuffer, in Buffer indexBuffer, uint indicies, int idx)
		{
			context.vk.CmdBindVertexBuffers(renderTarget.frame.commandBuffer, 0, [vertexBuffer], [0]);
			context.vk.CmdBindIndexBuffer(renderTarget.frame.commandBuffer, indexBuffer, 0, IndexType.Uint16);

			context.vk.CmdDrawIndexed(renderTarget.frame.commandBuffer, indicies, 1, 0, 0, 0);
		}

		public void EndRenderPass(VkContext context)
        {
            FinishRender(context, renderTarget.frame.commandBuffer);
            var result = context.vk.ResetFences(context.device, [renderTarget.frame.inFlight]);

            VulkanHelper.QueueSubmitCommands(context, graphicsQueue, renderTarget.frame.commandBuffer, renderTarget.frame.imageAvailable, renderTarget.frame.inFlight, PipelineStageFlags.ColorAttachmentOutputBit);
        }

        public void PresentRender(VkContext context)
        {
            // If RendeTargetManager failes to present recreate pipeline and try again
			if (!TRenderTargetManager.PresentTarget(context, ref renderTargetManager, ref renderTarget))
            {
				context.vk.DeviceWaitIdle(context.device);

				TRenderTargetManager.Dispose(context, ref renderTargetManager, commandPool);
                TRenderPassContainer.Dispose(context, ref renderPasses);
                TPipelineContainer.Dispose(context, ref pipelines);

				renderTargetManager = TRenderTargetManager.Create(context, commandPool);
				renderPasses = TRenderPassContainer.Create(context, TRenderTargetManager.GetRendePassInfo(context, ref renderTargetManager));

				RenderPass compatibleRenderPass = TRenderPassContainer.GetCompatibleRenderPass(ref renderPasses);
				pipelines = TPipelineContainer.Create<TDescriptorContainer>(context, TRenderPassContainer.GetCompatibleRenderPass(ref renderPasses), TRenderTargetManager.GetPipelineInfo(context, ref renderTargetManager));

				TRenderTargetManager.Init(context, ref renderTargetManager, compatibleRenderPass, commandPool);
			}

            renderTarget = default;
        }

        public void ClearDepthBuffer(VkContext context)
        {
            var renderArea = TRenderTargetManager.GetRenderArea(ref renderTargetManager);

            ClearRect clearArea = new ClearRect();
            clearArea.Rect = renderArea;
            clearArea.BaseArrayLayer = 0;
            clearArea.LayerCount = 1;

            ClearAttachment clearInfo = new ClearAttachment();
            clearInfo.AspectMask = ImageAspectFlags.DepthBit;
            clearInfo.ColorAttachment = 1;
            clearInfo.ClearValue = new ClearValue(depthStencil: new(1.0f, 0));

            context.vk.CmdClearAttachments(renderTarget.frame.commandBuffer, [clearInfo], [clearArea]);
        }

        public ref TUbo GetUbo<TUbo>(int idx)
            where TUbo : struct, IUniformBufferObject<TUbo>
        {
            return ref TRenderTargetManager.GetUbo<TUbo, TDescriptorContainer, TPipelineEnum>((uint)idx, ref renderTargetManager);
        }

        public DescriptorSet GetDescriptorSet(TPipelineEnum pipeline, int idx)
        {
            return TRenderTargetManager.GetDescriptorSet(pipeline, ref descriptorSets, (uint)idx, ref renderTargetManager);
		}

        public static RenderPipeline<TRenderTargetManager, TRenderPassInfo, TPipelineInfo, TDescriptorContainer, TPipelineContainer, TPipelineEnum, TRenderPassContainer, TRenderPassEnum> Create(VkContext context, CommandPool commandPool)
		{
            uint graphicsQueueFamily = VulkanHelper.GetGraphicsQueueFamily(context);
            Queue graphicsQueue = VulkanHelper.GetQueue(context, graphicsQueueFamily);

            var descriptorSets = TDescriptorContainer.Create<TRenderTargetManager, TRenderPassInfo, TPipelineInfo>(context);
			var renderTargetManager = TRenderTargetManager.Create(context, commandPool);
            var renderPassContainer = TRenderPassContainer.Create(context, TRenderTargetManager.GetRendePassInfo(context, ref renderTargetManager));

            RenderPass compatibleRenderPass = TRenderPassContainer.GetCompatibleRenderPass(ref renderPassContainer);
            var pipelineContainer = TPipelineContainer.Create<TDescriptorContainer>(context, TRenderPassContainer.GetCompatibleRenderPass(ref renderPassContainer), TRenderTargetManager.GetPipelineInfo(context, ref renderTargetManager));

            TRenderTargetManager.Init(context, ref renderTargetManager, compatibleRenderPass, commandPool);
			return new(graphicsQueue, commandPool, renderTargetManager, descriptorSets, pipelineContainer, renderPassContainer);
        }

        unsafe void BindDescriptorSets(VkContext context, in TPipelineEnum pipeline, int idx)
        {
			context.vk.CmdBindDescriptorSets(renderTarget.frame.commandBuffer, PipelineBindPoint.Graphics, TPipelineContainer.GetLayout(pipeline, ref pipelines), 0, 1, TRenderTargetManager.GetDescriptorSet(pipeline, ref descriptorSets, (uint)idx, ref renderTargetManager), 0, null);
		}

        static unsafe void BeginRenderCommand(VkContext context, CommandBuffer commandBuffer, RenderPass renderPass, Framebuffer framebuffer, ClearColorValue clearColor, Rect2D renderArea)
        {
            CommandBufferBeginInfo beginInfo = new();
            beginInfo.SType = StructureType.CommandBufferBeginInfo;
            beginInfo.Flags = CommandBufferUsageFlags.None;
            beginInfo.PInheritanceInfo = null;

            context.vk.BeginCommandBuffer(commandBuffer, beginInfo);

            RenderPassBeginInfo renderPassInfo = new();
            renderPassInfo.SType = StructureType.RenderPassBeginInfo;
            renderPassInfo.RenderPass = renderPass;
            renderPassInfo.Framebuffer = framebuffer;
            renderPassInfo.RenderArea = renderArea;

            ClearValue* clearColors = stackalloc ClearValue[2];
            clearColors[0] = new(clearColor);
            clearColors[1] = new(depthStencil: new(1.0f, 0));

            renderPassInfo.ClearValueCount = 2;
            renderPassInfo.PClearValues = clearColors;

            context.vk.CmdBeginRenderPass(commandBuffer, renderPassInfo, SubpassContents.Inline);
        }

        static unsafe void RenderSetViewportAndScissor(VkContext context, CommandBuffer commandBuffer, Rect2D renderArea)
        {
            Viewport viewport = new();
            viewport.X = 0;
            viewport.Y = 0;
            viewport.Width = renderArea.Extent.Width;
            viewport.Height = renderArea.Extent.Height;
            viewport.MinDepth = 0f;
            viewport.MaxDepth = 1f;

            context.vk.CmdSetViewport(commandBuffer, 0, 1, viewport);

            Rect2D scissor = new();
            scissor.Offset = new(0, 0);
            scissor.Extent = renderArea.Extent;

            context.vk.CmdSetScissor(commandBuffer, 0, 1, &scissor);
        }

        unsafe void RenderSetPolygonMode(VkContext context, CommandBuffer commandBuffer, Rect2D renderArea)
        {
        }

        static void FinishRender(VkContext context, CommandBuffer commandBuffer)
        {
            context.vk.CmdEndRenderPass(commandBuffer);

            var result = context.vk.EndCommandBuffer(commandBuffer);
            if (result != Result.Success)
                throw new Exception("Failed to end vkCommandBuffer");
        }
    }
}
