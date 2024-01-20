using EnCS;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;
using Image = Silk.NET.Vulkan.Image;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Engine
{
    public struct FrameInFlight<TDescriptorSet>
        where TDescriptorSet : struct, IDescriptorSet<TDescriptorSet>
    {
        public Semaphore imageAvailable;
        public Semaphore renderFinished;
        public Fence inFlight;
        public CommandBuffer commandBuffer;
        public FixedArray16<TDescriptorSet> descriptorData;

        public FrameInFlight(Semaphore imageAvailable, Semaphore renderFinished, Fence inFlight, CommandBuffer commandBuffer, FixedArray16<TDescriptorSet> descriptorData)
        {
            this.imageAvailable = imageAvailable;
            this.renderFinished = renderFinished;
            this.inFlight = inFlight;
            this.commandBuffer = commandBuffer;
            this.descriptorData = descriptorData;
        }
    }

    public struct RenderTarget<TDescriptorSet> where TDescriptorSet : struct, IDescriptorSet<TDescriptorSet>
    {
        public FrameInFlight<TDescriptorSet> frame;
        public Framebuffer framebuffer;
        public uint imageIndex; // TODO: Remove
    }

    public struct RenderPipelineNew<TRenderTargetManager, TRenderPassInfo, TPipelineInfo, TDescriptorSet, TPipelineContainer, TPipelineEnum, TRenderPassContainer, TRenderPassEnum>
		where TRenderTargetManager : struct, IRenderTargetManager<TRenderTargetManager, TDescriptorSet, TRenderPassInfo, TPipelineInfo>
		where TRenderPassInfo : struct
		where TPipelineInfo : struct
		where TDescriptorSet : struct, IDescriptorSet<TDescriptorSet>
		where TPipelineContainer : IPipelineContainer<TPipelineContainer, TPipelineInfo, TPipelineEnum>
		where TRenderPassContainer : IRenderPassContainer<TRenderPassContainer, TRenderPassInfo, TRenderPassEnum>
		where TPipelineEnum : Enum
		where TRenderPassEnum : Enum
	{
        TRenderTargetManager renderTargetManager;

		Queue graphicsQueue;

        TPipelineContainer pipelines;
        TRenderPassContainer renderPasses;

		ClearColorValue clearColor;
		RenderTarget<TDescriptorSet> renderTarget;

        public RenderPipelineNew(Queue graphicsQueue, TRenderTargetManager renderTargetManager, TPipelineContainer layers, TRenderPassContainer renderPasses)
        {
			this.graphicsQueue = graphicsQueue;
            this.renderTargetManager = renderTargetManager;
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

		public void StartRenderPass(VkContext context, TRenderPassEnum renderPass, TPipelineEnum pipeline)
		{
            var renderArea = TRenderTargetManager.GetRenderArea(ref renderTargetManager);
			var vkRenderPass = TRenderPassContainer.Get(renderPass, ref renderPasses);

            VulkanHelper.WaitForFence(context, renderTarget.frame.inFlight);
            context.vk.ResetCommandBuffer(renderTarget.frame.commandBuffer, CommandBufferResetFlags.ReleaseResourcesBit);

            BeginRenderCommand(context, renderTarget.frame.commandBuffer, vkRenderPass, renderTarget.framebuffer, clearColor, renderArea);
            RenderSetViewportAndScissor(context, renderTarget.frame.commandBuffer, renderArea);

            context.vk.CmdBindPipeline(renderTarget.frame.commandBuffer, PipelineBindPoint.Graphics, TPipelineContainer.Get(pipeline, ref pipelines));
        }

        public unsafe void Render(VkContext context, TPipelineEnum pipeline, Buffer vertexBuffer, Buffer indexBuffer, uint indicies, int idx)
        {
            context.vk.CmdBindDescriptorSets(renderTarget.frame.commandBuffer, PipelineBindPoint.Graphics, TPipelineContainer.GetLayout(pipeline, ref pipelines), 0, 1, TDescriptorSet.GetDescriptorSet(ref renderTarget.frame.descriptorData[idx]), 0, null);

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
			TRenderTargetManager.PresentTarget(context, ref renderTargetManager, ref renderTarget);
            renderTarget = default;
        }

        // TODO: Improve
        public unsafe ref TDescriptorSet GetDescriptor(VkContext context, int idx)
        {
			return ref renderTarget.frame.descriptorData[idx];
        }

        public static RenderPipelineNew<TRenderTargetManager, TRenderPassInfo, TPipelineInfo, TDescriptorSet, TPipelineContainer, TPipelineEnum, TRenderPassContainer, TRenderPassEnum> Create(VkContext context, TRenderTargetManager renderTargetManager)
		{
            uint graphicsQueueFamily = VulkanHelper.GetGraphicsQueueFamily(context);
            Queue graphicsQueue = VulkanHelper.GetQueue(context, graphicsQueueFamily);

            return new(graphicsQueue, renderTargetManager, TPipelineContainer.Create(context, TDescriptorSet.GetLayout(context), TRenderTargetManager.GetPipelineInfo(context, ref renderTargetManager)), TRenderPassContainer.Create(context, TRenderTargetManager.GetRendePassInfo(context, ref renderTargetManager)));
        }

        unsafe void BeginRenderCommand(VkContext context, CommandBuffer commandBuffer, RenderPass renderPass, Framebuffer framebuffer, ClearColorValue clearColor, Rect2D renderArea)
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

        unsafe void RenderSetViewportAndScissor(VkContext context, CommandBuffer commandBuffer, Rect2D renderArea)
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

        void FinishRender(VkContext context, CommandBuffer commandBuffer)
        {
            context.vk.CmdEndRenderPass(commandBuffer);

            var result = context.vk.EndCommandBuffer(commandBuffer);
            if (result != Result.Success)
                throw new Exception("Failed to end vkCommandBuffer");
        }
    }
}
