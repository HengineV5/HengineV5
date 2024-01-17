using Engine.Graphics;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System.Runtime.InteropServices;
using Image = Silk.NET.Vulkan.Image;
using Semaphore = Silk.NET.Vulkan.Semaphore;
using Buffer = Silk.NET.Vulkan.Buffer;
using System.Runtime.CompilerServices;
using Engine.Components.Graphics;
using System.Numerics;
using EnCS;
using static Engine.RenderPipeline;

namespace Engine
{
	public interface IRenderTargetManager<TSelf, TDescriptorSet, TRenderPassInfo, TPipelineInfo>
		where TSelf : struct, IRenderTargetManager<TSelf, TDescriptorSet, TRenderPassInfo, TPipelineInfo>
		where TDescriptorSet : struct, IDescriptorSet<TDescriptorSet>
		where TRenderPassInfo : struct
		where TPipelineInfo : struct
	{
		static abstract RenderTarget<TDescriptorSet> AquireRenderTarget(VkContext context, ref TSelf self);

		static abstract void PresentTarget(VkContext context, ref TSelf self, ref RenderTarget<TDescriptorSet> renderTarget);

		static abstract TRenderPassInfo GetRendePassInfo(VkContext context, ref TSelf self);

		static abstract TPipelineInfo GetPipelineInfo(VkContext context, ref TSelf self);

		static abstract Rect2D GetRenderArea(ref TSelf self);
	}

	public interface IDescriptorSet<TSelf> where TSelf : struct, IDescriptorSet<TSelf>
	{
		static abstract TSelf Create(VkContext context, DescriptorPool descriptorPool);

		static abstract DescriptorSet GetDescriptorSet(ref TSelf self);

		static abstract DescriptorSetLayout GetLayout(VkContext context);

		static abstract DescriptorPool GetPool(VkContext context, uint count);
	}

	public interface IPipelineContainer<TSelf, TPipelineInfo, TDescriptorSet, TEnum>
		where TSelf : IPipelineContainer<TSelf, TPipelineInfo, TDescriptorSet, TEnum>
		where TPipelineInfo : struct
		where TDescriptorSet : struct, IDescriptorSet<TDescriptorSet>
		where TEnum : Enum
    {
        static abstract TSelf Create(VkContext context, DescriptorSetLayout descriptorSetLayout, in TPipelineInfo pipelineInfo);

        static abstract Pipeline Get(TEnum mask, ref TSelf self);

        static abstract PipelineLayout GetLayout(TEnum mask, ref TSelf self);
    }

    public interface IRenderPassContainer<TSelf, TRenderPassInfo, TEnum>
		where TSelf : IRenderPassContainer<TSelf, TRenderPassInfo, TEnum>
		where TRenderPassInfo : struct
        where TEnum : Enum
    {
        static abstract TSelf Create(VkContext context, in TRenderPassInfo renderPassInfo);

		static abstract RenderPass Get(TEnum mask, ref TSelf self);
    }

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
		public uint imageIndex;
    }

	public struct SwapchainRenderPassInfo
	{
		public Format colorFormat;
		public Format depthFormat;

        public SwapchainRenderPassInfo(Format colorFormat, Format depthFormat)
        {
            this.colorFormat = colorFormat;
            this.depthFormat = depthFormat;
        }
    }

	public struct SwapchainPipelineInfo
	{
		public Extent2D extent;
		public RenderPass compatibleRenderPass;

        public SwapchainPipelineInfo(Extent2D extent, RenderPass compatibleRenderPass)
        {
            this.extent = extent;
            this.compatibleRenderPass = compatibleRenderPass;
        }
    }

	public struct SwapchainRenderTargetManager<TDescriptorSet> : IRenderTargetManager<SwapchainRenderTargetManager<TDescriptorSet>, TDescriptorSet, SwapchainRenderPassInfo, SwapchainPipelineInfo>
		where TDescriptorSet : struct, IDescriptorSet<TDescriptorSet>
	{
        const int MAX_FRAMES_IN_FLIGHT = 1;

		RenderPass compatibleRenderPass;
		Swapchain swapchain;

		Memory<ImageView> imageViews;
		Memory<Framebuffer> frameBuffers;
		Memory<FrameInFlight<TDescriptorSet>> framesInFlight;

		int currentFrame;

        public SwapchainRenderTargetManager(Swapchain swapchain, RenderPass compatibleRenderPass, Memory<ImageView> imageViews, Memory<Framebuffer> frameBuffers, Memory<FrameInFlight<TDescriptorSet>> framesInFlight)
        {
			this.swapchain = swapchain;
			this.compatibleRenderPass = compatibleRenderPass;
			this.imageViews = imageViews;
			this.frameBuffers = frameBuffers;
			this.framesInFlight = framesInFlight;
			this.currentFrame = 0;
        }

        public static RenderTarget<TDescriptorSet> AquireRenderTarget(VkContext context, ref SwapchainRenderTargetManager<TDescriptorSet> self)
        {
            var renderTarget = new RenderTarget<TDescriptorSet>()
			{
				frame = self.framesInFlight.Span[self.currentFrame],
			};

            VulkanHelper.WaitForFence(context, renderTarget.frame.inFlight);

            var aquireResult = self.swapchain.AcquireNextImageIndex(context, renderTarget.frame.imageAvailable, out renderTarget.imageIndex);
			renderTarget.framebuffer = self.frameBuffers.Span[(int)renderTarget.imageIndex];

            //Console.WriteLine($"A: {renderTarget.imageIndex}, F: {self.currentFrame}");

            //if (aquireResult == Result.ErrorOutOfDateKhr)
            if (aquireResult != Result.Success)
				throw new Exception();
            //return aquireResult;

            return renderTarget;
        }

		public static SwapchainRenderTargetManager<TDescriptorSet> Create(VkContext context, Swapchain swapchain, RenderPass compatibleRenderPass, CommandPool commandPool)
		{
            Memory<ImageView> swapchainImages = new ImageView[swapchain.GetImageCount()];
            swapchain.GetImages(context, swapchainImages.Span);

            Memory<Framebuffer> frameBuffers = new Framebuffer[swapchain.GetImageCount()];
            for (int i = 0; i < frameBuffers.Span.Length; i++)
            {
                frameBuffers.Span[i] = VulkanHelper.CreateFrameBuffer(context, [swapchainImages.Span[i], swapchain.GetDepthImage()], compatibleRenderPass, swapchain.GetExtent());
            }

			DescriptorPool descriptorPool = TDescriptorSet.GetPool(context, MAX_FRAMES_IN_FLIGHT * 16);

            Memory<FrameInFlight<TDescriptorSet>> framesInFlight = new FrameInFlight<TDescriptorSet>[MAX_FRAMES_IN_FLIGHT];
            for (int i = 0; i < framesInFlight.Span.Length; i++)
            {
                FixedArray16<TDescriptorSet> descriptorSets = new FixedArray16<TDescriptorSet>();

				for (int a = 0; a < 16; a++)
				{
					descriptorSets[a] = TDescriptorSet.Create(context, descriptorPool);
                }

                framesInFlight.Span[i] = new FrameInFlight<TDescriptorSet>(
					VulkanHelper.CreateSemaphore(context),
					VulkanHelper.CreateSemaphore(context),
					VulkanHelper.CreateFence(context, FenceCreateFlags.SignaledBit),
                    VulkanHelper.CreateCommandBuffer(context, commandPool),
                    descriptorSets
                );
            }

            return new SwapchainRenderTargetManager<TDescriptorSet>(swapchain, compatibleRenderPass, swapchainImages, frameBuffers, framesInFlight);
        }

		// TODO: Add image index as generic parameter in RenderTarget
        public static void PresentTarget(VkContext context, ref SwapchainRenderTargetManager<TDescriptorSet> self, ref RenderTarget<TDescriptorSet> renderTarget)
        {
            VulkanHelper.QueuePresent(context, self.swapchain.GetPresentQueue(), self.swapchain.GetSwapchain(), renderTarget.imageIndex, renderTarget.frame.imageAvailable);
            //Console.WriteLine($"P: {renderTarget.imageIndex}, F: {self.currentFrame}");

            // TODO: Improve
            self.currentFrame = (self.currentFrame + 1) % MAX_FRAMES_IN_FLIGHT;
        }

        public static SwapchainPipelineInfo GetPipelineInfo(VkContext context, ref SwapchainRenderTargetManager<TDescriptorSet> self)
        {
			return new SwapchainPipelineInfo(self.swapchain.GetExtent(), self.compatibleRenderPass);
        }

        public static SwapchainRenderPassInfo GetRendePassInfo(VkContext context, ref SwapchainRenderTargetManager<TDescriptorSet> self)
        {
			return new SwapchainRenderPassInfo(self.swapchain.GetSurfaceFormat().Format, Swapchain.GetDepthFormat(context));
        }

        public static Rect2D GetRenderArea(ref SwapchainRenderTargetManager<TDescriptorSet> self)
        {
			return new(new(), self.swapchain.GetExtent());
        }
    }

	public struct RenderPipelineNew<TRenderTargetManager, TRenderPassInfo, TPipelineInfo, TDescriptorSet, TPipelineContainer, TPipelineEnum, TRenderPassContainer, TRenderPassEnum>
		where TRenderTargetManager : struct, IRenderTargetManager<TRenderTargetManager, TDescriptorSet, TRenderPassInfo, TPipelineInfo>
		where TRenderPassInfo : struct
		where TPipelineInfo : struct
		where TDescriptorSet : struct, IDescriptorSet<TDescriptorSet>
		where TPipelineContainer : IPipelineContainer<TPipelineContainer, TPipelineInfo, TDescriptorSet, TPipelineEnum>
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

    public struct DefaultDescriptorSet : IDescriptorSet<DefaultDescriptorSet>
    {
		public VulkanShaderInput shaderInput;
		public DescriptorSet descriptorSet;

        public DefaultDescriptorSet(DescriptorSet descriptorSet, VulkanShaderInput shaderInput)
        {
            this.descriptorSet = descriptorSet;
            this.shaderInput = shaderInput;
        }

        public static unsafe DefaultDescriptorSet Create(VkContext context, DescriptorPool descriptorPool)
        {
			DescriptorSetLayout layout = GetLayout(context);
            var descriptorSet = new DefaultDescriptorSet(new DescriptorSet(), new VulkanShaderInput());

            DescriptorSetAllocateInfo allocInfo = new();
            allocInfo.SType = StructureType.DescriptorSetAllocateInfo;
            allocInfo.DescriptorPool = descriptorPool;
            allocInfo.DescriptorSetCount = 1;
            allocInfo.PSetLayouts = &layout;

            var result = context.vk.AllocateDescriptorSets(context.device, allocInfo, out descriptorSet.descriptorSet);
            if (result != Result.Success)
                throw new Exception("Failed to allocate vkDescriptorSets");

            Buffer uniformBuffer = VulkanHelper.CreateBuffer(context, BufferUsageFlags.UniformBufferBit, 704);
            DeviceMemory uniformBuffersMemory = VulkanHelper.CreateBufferMemory(context, uniformBuffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

            void* dataPtr;
            context.vk.MapMemory(context.device, uniformBuffersMemory, 0, 704, 0, &dataPtr);

			// TODO: Reorder
            var uniformBufferBuilder = new UniformBufferBuilder(descriptorSet.descriptorSet, uniformBuffer)
                        .Variable<UniformBufferObject>(0)
                        //.Variable<Material>(2)
                        .Variable<PbrMaterial>(7)
                        .Array<Light>(8, 4);

            descriptorSet.shaderInput.ubo = uniformBufferBuilder.GetElement<UniformBufferObject>(dataPtr, 0);
            //framesInFlight.Span[i].uboMemories[a].material = uniformBufferBuilder.GetElement<Material>(dataPtr, 1);
            descriptorSet.shaderInput.material = uniformBufferBuilder.GetElement<PbrMaterial>(dataPtr, 1);
            for (int b = 0; b < 4; b++)
            {
                descriptorSet.shaderInput.lights[b] = uniformBufferBuilder.GetElement<Light>(dataPtr, 2 + (uint)b);
            }

            uniformBufferBuilder.UpdateDescriptorSet(context);
			return descriptorSet;
        }

        public static DescriptorSet GetDescriptorSet(ref DefaultDescriptorSet self)
        {
			return self.descriptorSet;
        }

        public static DescriptorSetLayout GetLayout(VkContext context)
        {
            return RenderPipeline.CreateDescriptorSetLayout(context);
        }

        public static DescriptorPool GetPool(VkContext context, uint count)
        {
            return VulkanHelper.CreateDescriptorPool(context, count);
        }
    }

    public struct RenderLayer
	{
		public Shader shader;
		public Pipeline pipeline;
		public PipelineLayout layout;

        public RenderLayer(Shader shader, Pipeline pipeline, PipelineLayout layout)
        {
            this.shader = shader;
            this.pipeline = pipeline;
            this.layout = layout;
        }

        public static RenderLayer Create(VkContext context, Shader shader, PipelineLayout layout, in SwapchainPipelineInfo info)
		{
            return new RenderLayer(shader, RenderPipeline.CreateGraphicsPipeline(context, info.extent, layout, info.compatibleRenderPass, shader), layout);
		}
    }

	[Flags]
	public enum PipelineContainerLayer
	{
		Skybox,
		Pbr
	}

	public struct PipelineContainer : IPipelineContainer<PipelineContainer, SwapchainPipelineInfo, DefaultDescriptorSet, PipelineContainerLayer>
	{
		public RenderLayer skyboxLayer;
		public RenderLayer pbrLayer;

        public PipelineContainer(RenderLayer skyboxLayer, RenderLayer pbrLayer)
        {
            this.skyboxLayer = skyboxLayer;
            this.pbrLayer = pbrLayer;
        }

        public static PipelineContainer Create(VkContext context, DescriptorSetLayout descriptorSetLayout, in SwapchainPipelineInfo info)
		{
			var pipelineLayout = RenderPipeline.CreatePipelineLayout(context, descriptorSetLayout);

            var skyboxShader = Shader.FromFiles("Shaders/Pbr/SkyboxVert.spv", "Shaders/Pbr/SkyboxFrag.spv");
			var skyboxPipeline = RenderLayer.Create(context, skyboxShader, pipelineLayout, info);

            var pbrShader = Shader.FromFiles("Shaders/Pbr/PbrVert.spv", "Shaders/Pbr/PbrFrag.spv");
            var pbrPipeline = RenderLayer.Create(context, pbrShader, pipelineLayout, info);

			return new PipelineContainer(skyboxPipeline, pbrPipeline);
		}

        public static Pipeline Get(PipelineContainerLayer layer, ref PipelineContainer self)
        {
			switch (layer)
			{
				case PipelineContainerLayer.Skybox:
                    return self.skyboxLayer.pipeline;
                case PipelineContainerLayer.Pbr:
                    return self.pbrLayer.pipeline;
                default:
                    throw new Exception();
            }
		}

        public static PipelineLayout GetLayout(PipelineContainerLayer layer, ref PipelineContainer self)
        {
            switch (layer)
            {
                case PipelineContainerLayer.Skybox:
                    return self.skyboxLayer.layout;
                case PipelineContainerLayer.Pbr:
                    return self.pbrLayer.layout;
                default:
                    throw new Exception();
            }
        }
    }

	public enum RenderPassId
	{
		Skybox,
		Mesh
	}

    public struct RenderPassContainer : IRenderPassContainer<RenderPassContainer, SwapchainRenderPassInfo, RenderPassId>
    {
		public RenderPass skyboxRenderPass;
		public RenderPass meshRenderPass;

        public RenderPassContainer(RenderPass skyboxRenderPass, RenderPass meshRenderPass)
        {
            this.skyboxRenderPass = skyboxRenderPass;
            this.meshRenderPass = meshRenderPass;
        }

        public static RenderPassContainer Create(VkContext context, in SwapchainRenderPassInfo renderPassInfo)
        {
            var skyboxRenderPass = CreateSkyboxRenderPass(context, renderPassInfo.colorFormat, renderPassInfo.depthFormat);
            var meshRenderPass = CreateMeshRenderPass(context, renderPassInfo.colorFormat, renderPassInfo.depthFormat);

			return new RenderPassContainer(skyboxRenderPass, meshRenderPass);
        }

        public static RenderPass Get(RenderPassId id, ref RenderPassContainer self)
        {
			switch (id)
			{
				case RenderPassId.Skybox:
					return self.skyboxRenderPass;
				case RenderPassId.Mesh:
					return self.meshRenderPass;
				default:
					throw new Exception();
			}
		}
    }

	public struct RenderPipeline
	{
		public struct FrameData
		{
			public Semaphore imageAvailable;
			public Semaphore renderFinished;
			public Fence inFlight;

			public FixedArray16<DescriptorSet> descriptorSets;
			public FixedArray16<VulkanShaderInput> uboMemories;
			public CommandBuffer commandBuffer;
		}

		struct PushConstant
		{
			public Matrix4x4 model;
		}

		const int MAX_FRAMES_IN_FLIGHT = 3;

		Swapchain swapchain;
		RenderPass skyboxRenderPass;
		RenderPass meshRenderPass;
		FixedArray8<Sampler> samplers;
		DescriptorSetLayout descriptorSetLayout;
		PipelineLayout pipelineLayout;
		Pipeline skyboxPipeline;
		Pipeline meshPipeline;

		Memory<ImageView> swapchainImages;
		Memory<Framebuffer> frameBuffers;
		Memory<FrameData> framesInFlight;

		int currentFrame;
		uint imageIndex;

		public RenderPipeline(Swapchain swapchain, RenderPass renderPass, RenderPass meshRenderPass, FixedArray8<Sampler> sampler, DescriptorSetLayout descriptorSetLayout, PipelineLayout pipelineLayout, Pipeline meshPipeline, Pipeline skyboxPipeline, Memory<ImageView> swapchainImages, Memory<Framebuffer> frameBuffers, Memory<FrameData> framesInFlight) : this()
		{
			this.swapchain = swapchain;
			this.skyboxRenderPass = renderPass;
			this.meshRenderPass = meshRenderPass;
			this.samplers = sampler;
			this.descriptorSetLayout = descriptorSetLayout;
			this.pipelineLayout = pipelineLayout;
			this.meshPipeline = meshPipeline;
			this.skyboxPipeline = skyboxPipeline;
			this.swapchainImages = swapchainImages;
			this.frameBuffers = frameBuffers;
			this.framesInFlight = framesInFlight;
			this.currentFrame = 0;
			this.imageIndex = 0;
		}

		public void Dispose(VkContext context)
		{
			/*
			for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
			{
				context.vk.DestroySemaphore(context.device, imageAvailableSemaphores.Span[i], null);
				context.vk.DestroySemaphore(context.device, renderFinishedSemaphores.Span[i], null);
				context.vk.DestroyFence(context.device, inFlightFences.Span[i], null);
			}

			DisposeSwapChain();

			context.vk.DestroyDescriptorSetLayout(context.device, descriptorSetLayout, null);

			context.vk.DestroyBuffer(context.device, vertexBuffer, null);
			context.vk.FreeMemory(context.device, vertexBufferMemory, null);

			for (int i = 0; i < uniformBuffers.Length; i++)
			{
				context.vk.DestroyBuffer(context.device, uniformBuffers.Span[i], null);
			}

			for (int i = 0; i < uniformBuffersMemory.Length; i++)
			{
				context.vk.FreeMemory(context.device, uniformBuffersMemory.Span[i], null);
			}

			context.vk.DestroyCommandPool(context.device, commandPool, null);

			context.vk.DestroyDescriptorPool(context.device, descriptorPool, null);
			context.vk.DestroyDescriptorSetLayout(context.device, descriptorSetLayout, null);

			context.vk.DestroySampler(context.device, sampler, null);

			context.vk.DestroyImageView(context.device, textureImageView, null);
			context.vk.DestroyImage(context.device, texture, null);
			context.vk.FreeMemory(context.device, textureMemory, null);

			context.vk.DestroyImageView(context.device, depthImageView, null);
			context.vk.DestroyImage(context.device, depthImage, null);
			context.vk.FreeMemory(context.device, depthImageMemory, null);

			context.vk.TryGetInstanceExtension(context.instance, out KhrSurface khrSurface);
			khrSurface.DestroySurface(context.instance, surface, null);

			context.vk.DestroyDevice(context.device, null);
			context.vk.DestroyInstance(context.instance, null);
			*/
		}

		unsafe void DisposeSwapchain(VkContext context, CommandPool commandPool)
		{
			swapchain.Dispose(context);

			foreach (var frameBuffer in frameBuffers.Span)
			{
				context.vk.DestroyFramebuffer(context.device, frameBuffer, null);
			}

			foreach (var imageView in swapchainImages.Span)
			{
				context.vk.DestroyImageView(context.device, imageView, null);
			}

			foreach (var frame in framesInFlight.Span)
			{
				context.vk.FreeCommandBuffers(context.device, commandPool, [frame.commandBuffer]);
			}

			context.vk.DestroyPipeline(context.device, meshPipeline, null);
			context.vk.DestroyPipelineLayout(context.device, pipelineLayout, null);
			context.vk.DestroyRenderPass(context.device, skyboxRenderPass, null);
		}

		public void RecreateSwapchain(VkContext context, SurfaceKHR surface, CommandPool commandPool)
		{
			context.vk.DeviceWaitIdle(context.device);

			swapchain.Dispose(context);
			DisposeSwapchain(context, commandPool);

			swapchain = Swapchain.Create(context, surface, commandPool);
            meshRenderPass = CreateMeshRenderPass(context, swapchain.GetSurfaceFormat().Format, Swapchain.GetDepthFormat(context));
            skyboxRenderPass = CreateSkyboxRenderPass(context, swapchain.GetSurfaceFormat().Format, Swapchain.GetDepthFormat(context));
			pipelineLayout = CreatePipelineLayout(context, descriptorSetLayout);

            var pbrShader = Shader.FromFiles("Shaders/Pbr/PbrVert.spv", "Shaders/Pbr/PbrFrag.spv");
            meshPipeline = CreateGraphicsPipeline(context, swapchain.GetExtent(), pipelineLayout, skyboxRenderPass, pbrShader);

            var shaderSkybox = Shader.FromFiles("Shaders/Pbr/SkyboxVert.spv", "Shaders/Pbr/SkyboxFrag.spv");
            skyboxPipeline = CreateGraphicsPipeline(context, swapchain.GetExtent(), pipelineLayout, skyboxRenderPass, shaderSkybox);

            swapchain.GetImages(context, swapchainImages.Span);
			for (int i = 0; i < frameBuffers.Span.Length; i++)
			{
				frameBuffers.Span[i] = VulkanHelper.CreateFrameBuffer(context, [swapchainImages.Span[i], swapchain.GetDepthImage()], skyboxRenderPass, swapchain.GetExtent());
			}

			for (int i = 0; i < framesInFlight.Span.Length; i++)
			{
				framesInFlight.Span[i].commandBuffer = VulkanHelper.CreateCommandBuffer(context, commandPool);
			}
		}

		public unsafe Result StartRender(VkContext context, ref UniformBufferObject ubo, ImageView skybox, Buffer vertexBuffer, Buffer indexBuffer, uint indicies)
		{
			ref FrameData frame = ref framesInFlight.Span[currentFrame];
			VulkanHelper.WaitForFence(context, frame.inFlight);

			var aquireResult = swapchain.AcquireNextImageIndex(context, frame.imageAvailable, out imageIndex);

			if (aquireResult == Result.ErrorOutOfDateKhr)
				return aquireResult;

			/*
			VulkanHelper.WaitForFence(context, frame.inFlight);
			context.vk.ResetCommandBuffer(frame.commandBuffer, CommandBufferResetFlags.ReleaseResourcesBit);

			System.Drawing.Color color = System.Drawing.Color.CornflowerBlue;
			ClearColorValue clearColor = new() { Float32_0 = color.R / 255f, Float32_1 = color.G / 255f, Float32_2 = color.B / 255f, Float32_3 = color.A / 255f };

			var renderArea = new Rect2D(new(), swapchain.GetExtent());

			Framebuffer framebuffer = frameBuffers.Span[(int)imageIndex];
			BeginRenderCommand(context, frame.commandBuffer, skyboxRenderPass, framebuffer, clearColor, renderArea);
			RenderSetViewportAndScissor(context, frame.commandBuffer, renderArea);

			context.vk.CmdBindPipeline(frame.commandBuffer, PipelineBindPoint.Graphics, skyboxPipeline);

            DescriptorImageInfo imageInfo = new();
            imageInfo.ImageLayout = ImageLayout.ReadOnlyOptimal;
            imageInfo.ImageView = skybox;
            imageInfo.Sampler = samplers[0];

            WriteDescriptorSet imageDescriptorWrite = new();
            imageDescriptorWrite.SType = StructureType.WriteDescriptorSet;
            imageDescriptorWrite.DstSet = frame.descriptorSets[15];
            imageDescriptorWrite.DstBinding = 6;
            imageDescriptorWrite.DstArrayElement = 0;
            imageDescriptorWrite.DescriptorType = DescriptorType.CombinedImageSampler;
            imageDescriptorWrite.DescriptorCount = 1;
            imageDescriptorWrite.PImageInfo = (DescriptorImageInfo*)Unsafe.AsPointer(ref imageInfo);

            context.vk.UpdateDescriptorSets(context.device, [imageDescriptorWrite], 0, null);

            frame.uboMemories[15].ubo.Value = ubo;

            context.vk.CmdBindDescriptorSets(frame.commandBuffer, PipelineBindPoint.Graphics, pipelineLayout, 0, 1, frame.descriptorSets[15], 0, null);

            context.vk.CmdBindVertexBuffers(frame.commandBuffer, 0, [vertexBuffer], [0]);
            context.vk.CmdBindIndexBuffer(frame.commandBuffer, indexBuffer, 0, IndexType.Uint16);

            context.vk.CmdDrawIndexed(frame.commandBuffer, indicies, 1, 0, 0, 0);

            FinishRender(context, frame.commandBuffer);
			var result = context.vk.ResetFences(context.device, [frame.inFlight]);

			VulkanHelper.QueueSubmitCommands(context, swapchain.GetGraphicsQueue(), frame.commandBuffer, frame.imageAvailable, frame.inFlight, PipelineStageFlags.ColorAttachmentOutputBit);
            //VulkanHelper.QueuePresent(context, swapchain.GetPresentQueue(), swapchain.GetSwapchain(), imageIndex, frame.imageAvailable);
			*/

            return aquireResult;
		}

		public unsafe void BeginRenderPass(VkContext context)
		{
			ref FrameData frame = ref framesInFlight.Span[currentFrame];

			VulkanHelper.WaitForFence(context, frame.inFlight);
			context.vk.ResetCommandBuffer(frame.commandBuffer, CommandBufferResetFlags.ReleaseResourcesBit);

			System.Drawing.Color color = System.Drawing.Color.CornflowerBlue;
			ClearColorValue clearColor = new() { Float32_0 = color.R / 255f, Float32_1 = color.G / 255f, Float32_2 = color.B / 255f, Float32_3 = color.A / 255f };

			var renderArea = new Rect2D(new(), swapchain.GetExtent());

			Framebuffer framebuffer = frameBuffers.Span[(int)imageIndex];
			BeginRenderCommand(context, frame.commandBuffer, meshRenderPass, framebuffer, clearColor, renderArea);
			RenderSetViewportAndScissor(context, frame.commandBuffer, renderArea);

			context.vk.CmdBindPipeline(frame.commandBuffer, PipelineBindPoint.Graphics, meshPipeline);
		}

		public unsafe void UpdateFrameDescriptorSet(VkContext context, ImageView texture, int idx, VkTextureBuffer albedo, VkTextureBuffer normal, VkTextureBuffer metallic, VkTextureBuffer roughness, VkTextureBuffer skybox)
		{
            ref FrameData frame = ref framesInFlight.Span[currentFrame];

			Span<DescriptorImageInfo> infos = stackalloc DescriptorImageInfo[6];
			Span<WriteDescriptorSet> descriptorWrites = stackalloc WriteDescriptorSet[6];

			{
				ref DescriptorImageInfo imageInfo = ref infos[0];
				imageInfo.ImageLayout = ImageLayout.ReadOnlyOptimal;
				imageInfo.ImageView = texture;
				imageInfo.Sampler = samplers[0];

				ref WriteDescriptorSet imageDescriptorWrite = ref descriptorWrites[0];
				imageDescriptorWrite.SType = StructureType.WriteDescriptorSet;
				imageDescriptorWrite.DstSet = frame.descriptorSets[idx];
				imageDescriptorWrite.DstBinding = 1;
				imageDescriptorWrite.DstArrayElement = 0;
				imageDescriptorWrite.DescriptorType = DescriptorType.CombinedImageSampler;
				imageDescriptorWrite.DescriptorCount = 1;
				imageDescriptorWrite.PImageInfo = (DescriptorImageInfo*)Unsafe.AsPointer(ref imageInfo);
			}

			{
				ref DescriptorImageInfo imageInfo = ref infos[1];
				imageInfo.ImageLayout = ImageLayout.ReadOnlyOptimal;
				imageInfo.ImageView = albedo.textureImageView;
				imageInfo.Sampler = samplers[1];

				ref WriteDescriptorSet imageDescriptorWrite = ref descriptorWrites[1];
				imageDescriptorWrite.SType = StructureType.WriteDescriptorSet;
				imageDescriptorWrite.DstSet = frame.descriptorSets[idx];
				imageDescriptorWrite.DstBinding = 2;
				imageDescriptorWrite.DstArrayElement = 0;
				imageDescriptorWrite.DescriptorType = DescriptorType.CombinedImageSampler;
				imageDescriptorWrite.DescriptorCount = 1;
				imageDescriptorWrite.PImageInfo = (DescriptorImageInfo*)Unsafe.AsPointer(ref imageInfo);
			}

			{
				ref DescriptorImageInfo imageInfo = ref infos[2];
				imageInfo.ImageLayout = ImageLayout.ReadOnlyOptimal;
				imageInfo.ImageView = normal.textureImageView;
				imageInfo.Sampler = samplers[2];

				ref WriteDescriptorSet imageDescriptorWrite = ref descriptorWrites[2];
				imageDescriptorWrite.SType = StructureType.WriteDescriptorSet;
				imageDescriptorWrite.DstSet = frame.descriptorSets[idx];
				imageDescriptorWrite.DstBinding = 3;
				imageDescriptorWrite.DstArrayElement = 0;
				imageDescriptorWrite.DescriptorType = DescriptorType.CombinedImageSampler;
				imageDescriptorWrite.DescriptorCount = 1;
				imageDescriptorWrite.PImageInfo = (DescriptorImageInfo*)Unsafe.AsPointer(ref imageInfo);
			}

			{
				ref DescriptorImageInfo imageInfo = ref infos[3];
				imageInfo.ImageLayout = ImageLayout.ReadOnlyOptimal;
				imageInfo.ImageView = metallic.textureImageView;
				imageInfo.Sampler = samplers[3];

				ref WriteDescriptorSet imageDescriptorWrite = ref descriptorWrites[3];
				imageDescriptorWrite.SType = StructureType.WriteDescriptorSet;
				imageDescriptorWrite.DstSet = frame.descriptorSets[idx];
				imageDescriptorWrite.DstBinding = 4;
				imageDescriptorWrite.DstArrayElement = 0;
				imageDescriptorWrite.DescriptorType = DescriptorType.CombinedImageSampler;
				imageDescriptorWrite.DescriptorCount = 1;
				imageDescriptorWrite.PImageInfo = (DescriptorImageInfo*)Unsafe.AsPointer(ref imageInfo);
			}

			{
				ref DescriptorImageInfo imageInfo = ref infos[4];
				imageInfo.ImageLayout = ImageLayout.ReadOnlyOptimal;
				imageInfo.ImageView = roughness.textureImageView;
				imageInfo.Sampler = samplers[4];

				ref WriteDescriptorSet imageDescriptorWrite = ref descriptorWrites[4];
				imageDescriptorWrite.SType = StructureType.WriteDescriptorSet;
				imageDescriptorWrite.DstSet = frame.descriptorSets[idx];
				imageDescriptorWrite.DstBinding = 5;
				imageDescriptorWrite.DstArrayElement = 0;
				imageDescriptorWrite.DescriptorType = DescriptorType.CombinedImageSampler;
				imageDescriptorWrite.DescriptorCount = 1;
				imageDescriptorWrite.PImageInfo = (DescriptorImageInfo*)Unsafe.AsPointer(ref imageInfo);
			}

			{
				ref DescriptorImageInfo imageInfo = ref infos[5];
				imageInfo.ImageLayout = ImageLayout.ReadOnlyOptimal;
				imageInfo.ImageView = skybox.textureImageView;
				imageInfo.Sampler = samplers[5];

				ref WriteDescriptorSet imageDescriptorWrite = ref descriptorWrites[5];
				imageDescriptorWrite.SType = StructureType.WriteDescriptorSet;
				imageDescriptorWrite.DstSet = frame.descriptorSets[idx];
				imageDescriptorWrite.DstBinding = 6;
				imageDescriptorWrite.DstArrayElement = 0;
				imageDescriptorWrite.DescriptorType = DescriptorType.CombinedImageSampler;
				imageDescriptorWrite.DescriptorCount = 1;
				imageDescriptorWrite.PImageInfo = (DescriptorImageInfo*)Unsafe.AsPointer(ref imageInfo);
			}

			context.vk.UpdateDescriptorSets(context.device, descriptorWrites, 0, null);
		}

		public unsafe void Render(VkContext context, ref UniformBufferObject ubo, in PbrMaterial material, in FixedArray4<Light> lights, Buffer vertexBuffer, Buffer indexBuffer, uint indicies, int idx)
		{
			ref FrameData frame = ref framesInFlight.Span[currentFrame];
			frame.uboMemories[idx].ubo.Value = ubo;
			frame.uboMemories[idx].material.Value = material;
			for (int i = 0; i < 4; i++)
			{
				frame.uboMemories[idx].lights[i].Value = lights[i];
			}

			context.vk.CmdBindDescriptorSets(frame.commandBuffer, PipelineBindPoint.Graphics, pipelineLayout, 0, 1, frame.descriptorSets[idx], 0, null);

			context.vk.CmdBindVertexBuffers(frame.commandBuffer, 0, [vertexBuffer], [0]);
			context.vk.CmdBindIndexBuffer(frame.commandBuffer, indexBuffer, 0, IndexType.Uint16);

			context.vk.CmdDrawIndexed(frame.commandBuffer, indicies, 1, 0, 0, 0);
		}

		public void EndRenderPass(VkContext context)
		{
            ref FrameData frame = ref framesInFlight.Span[currentFrame];

			FinishRender(context, frame.commandBuffer);
			var result = context.vk.ResetFences(context.device, [frame.inFlight]);

            VulkanHelper.QueueSubmitCommands(context, swapchain.GetGraphicsQueue(), frame.commandBuffer, frame.imageAvailable, frame.inFlight, PipelineStageFlags.ColorAttachmentOutputBit);
			//VulkanHelper.WaitForSemaphore(context, frame.imageAvailable);
			/*
			VulkanHelper.QueuePresent(context, swapchain.GetPresentQueue(), swapchain.GetSwapchain(), imageIndex, frame.imageAvailable);

			// TODO: Improve
			currentFrame = (currentFrame + 1) % MAX_FRAMES_IN_FLIGHT;
			*/
		}

		public void PresentRender(VkContext context)
		{
			ref FrameData frame = ref framesInFlight.Span[currentFrame];

			VulkanHelper.QueuePresent(context, swapchain.GetPresentQueue(), swapchain.GetSwapchain(), imageIndex, frame.imageAvailable);

			// TODO: Improve
			currentFrame = (currentFrame + 1) % MAX_FRAMES_IN_FLIGHT;
			/*
			*/
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
			clearColors[0] = new (clearColor);
			clearColors[1] = new (depthStencil: new(1.0f, 0));

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

		public static RenderPipeline Create(VkContext context, SurfaceKHR surface, CommandPool commandPool)
		{
			Swapchain swapchain = Swapchain.Create(context, surface, commandPool);

			RenderPass renderPass = CreateSkyboxRenderPass(context, swapchain.GetSurfaceFormat().Format, Swapchain.GetDepthFormat(context));
			RenderPass meshRenderPass = CreateMeshRenderPass(context, swapchain.GetSurfaceFormat().Format, Swapchain.GetDepthFormat(context));

			FixedArray8<Sampler> samplers = new FixedArray8<Sampler>();
			for (int i = 0; i < 8; i++)
			{
				samplers[i] = VulkanHelper.CreateSampler(context);
			}
			//Sampler sampler = VulkanHelper.CreateSampler(context);

			DescriptorSetLayout descriptorSetLayout = CreateDescriptorSetLayout(context);
			PipelineLayout pipelineLayout = CreatePipelineLayout(context, descriptorSetLayout);
            var pbrShader = Shader.FromFiles("Shaders/Pbr/PbrVert.spv", "Shaders/Pbr/PbrFrag.spv");
            Pipeline meshPipeline = CreateGraphicsPipeline(context, swapchain.GetExtent(), pipelineLayout, renderPass, pbrShader);

            var shaderSkybox = Shader.FromFiles("Shaders/Pbr/SkyboxVert.spv", "Shaders/Pbr/SkyboxFrag.spv");
            Pipeline skyboxPipeline = CreateGraphicsPipeline(context, swapchain.GetExtent(), pipelineLayout, renderPass, shaderSkybox);

			Memory<ImageView> swapchainImages = new ImageView[swapchain.GetImageCount()];
			swapchain.GetImages(context, swapchainImages.Span);

			Memory<Framebuffer> frameBuffers = new Framebuffer[swapchain.GetImageCount()];
			for (int i = 0; i < frameBuffers.Span.Length; i++)
			{
				frameBuffers.Span[i] = VulkanHelper.CreateFrameBuffer(context, [swapchainImages.Span[i], swapchain.GetDepthImage()], renderPass, swapchain.GetExtent());
			}

			DescriptorPool descriptorPool = VulkanHelper.CreateDescriptorPool(context, MAX_FRAMES_IN_FLIGHT * 16);
			Memory<FrameData> framesInFlight = CreateFramesInFlight(context, descriptorPool, commandPool, descriptorSetLayout);

			return new RenderPipeline(swapchain, renderPass, meshRenderPass, samplers, descriptorSetLayout, pipelineLayout, meshPipeline, skyboxPipeline, swapchainImages, frameBuffers, framesInFlight);
		}

		static unsafe Memory<FrameData> CreateFramesInFlight(VkContext context, DescriptorPool descriptorPool, CommandPool commandPool, DescriptorSetLayout layout)
		{
			Memory<FrameData> framesInFlight = new FrameData[MAX_FRAMES_IN_FLIGHT];
			for (int i = 0; i < framesInFlight.Length; i++)
			{
				framesInFlight.Span[i].imageAvailable = VulkanHelper.CreateSemaphore(context);
				framesInFlight.Span[i].renderFinished = VulkanHelper.CreateSemaphore(context);
				framesInFlight.Span[i].inFlight = VulkanHelper.CreateFence(context, FenceCreateFlags.SignaledBit);
				framesInFlight.Span[i].commandBuffer = VulkanHelper.CreateCommandBuffer(context, commandPool);

				for (int a = 0; a < 16; a++)
				{
					Buffer uniformBuffer = VulkanHelper.CreateBuffer(context, BufferUsageFlags.UniformBufferBit, 704);
					DeviceMemory uniformBuffersMemory = VulkanHelper.CreateBufferMemory(context, uniformBuffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

					framesInFlight.Span[i].uboMemories[a] = new VulkanShaderInput();

					DescriptorSetAllocateInfo allocInfo = new();
					allocInfo.SType = StructureType.DescriptorSetAllocateInfo;
					allocInfo.DescriptorPool = descriptorPool;
					allocInfo.DescriptorSetCount = 1;
					allocInfo.PSetLayouts = &layout;

                    var result = context.vk.AllocateDescriptorSets(context.device, allocInfo, out DescriptorSet descriptorSet);
					if (result != Result.Success)
						throw new Exception("Failed to allocate vkDescriptorSets");

					var uniformBufferBuilder = new UniformBufferBuilder(descriptorSet, uniformBuffer)
						.Variable<UniformBufferObject>(0)
						//.Variable<Material>(2)
						.Variable<PbrMaterial>(7)
						.Array<Light>(8, 4);

					void* dataPtr;
					context.vk.MapMemory(context.device, uniformBuffersMemory, 0, 704, 0, &dataPtr);

					framesInFlight.Span[i].uboMemories[a].ubo = uniformBufferBuilder.GetElement<UniformBufferObject>(dataPtr, 0);
					//framesInFlight.Span[i].uboMemories[a].material = uniformBufferBuilder.GetElement<Material>(dataPtr, 1);
					framesInFlight.Span[i].uboMemories[a].material = uniformBufferBuilder.GetElement<PbrMaterial>(dataPtr, 1);
					for (int b = 0; b < 4; b++)
					{
						framesInFlight.Span[i].uboMemories[a].lights[b] = uniformBufferBuilder.GetElement<Light>(dataPtr, 2 + (uint)b);
					}

					uniformBufferBuilder.UpdateDescriptorSet(context);

					framesInFlight.Span[i].descriptorSets[a] = descriptorSet;
				}
			}

			return framesInFlight;
		}

		// TODO: Make private
		public static unsafe RenderPass CreateSkyboxRenderPass(VkContext context, Format colorFormat, Format depthFormat)
		{
			AttachmentDescription colorAttachment = new();
			colorAttachment.Format = colorFormat;
			colorAttachment.Samples = SampleCountFlags.Count1Bit;
			colorAttachment.LoadOp = AttachmentLoadOp.Clear;
			colorAttachment.StoreOp = AttachmentStoreOp.Store;
			colorAttachment.StencilLoadOp = AttachmentLoadOp.DontCare;
			colorAttachment.StencilStoreOp = AttachmentStoreOp.DontCare;
			colorAttachment.InitialLayout = ImageLayout.Undefined;
			colorAttachment.FinalLayout = ImageLayout.PresentSrcKhr;

			AttachmentDescription depthAttatchment = new();
			depthAttatchment.Format = depthFormat;
			depthAttatchment.Samples = SampleCountFlags.Count1Bit;
			depthAttatchment.LoadOp = AttachmentLoadOp.Clear;
			depthAttatchment.StoreOp = AttachmentStoreOp.None;
			depthAttatchment.StencilLoadOp = AttachmentLoadOp.DontCare;
			depthAttatchment.StencilStoreOp = AttachmentStoreOp.DontCare;
			depthAttatchment.InitialLayout = ImageLayout.Undefined;
			depthAttatchment.FinalLayout = ImageLayout.DepthStencilAttachmentOptimal;

			AttachmentReference colorAttachmentRef = new();
			colorAttachmentRef.Attachment = 0;
			colorAttachmentRef.Layout = ImageLayout.ColorAttachmentOptimal;

			AttachmentReference depthAttachmentRef = new();
			depthAttachmentRef.Attachment = 1;
			depthAttachmentRef.Layout = ImageLayout.DepthStencilAttachmentOptimal;

			SubpassDescription subpass = new();
			subpass.PipelineBindPoint = PipelineBindPoint.Graphics;
			subpass.ColorAttachmentCount = 1;
			subpass.PColorAttachments = &colorAttachmentRef;
			subpass.PDepthStencilAttachment = &depthAttachmentRef;

			SubpassDependency dependency = new();
			dependency.SrcSubpass = Vk.SubpassExternal;
			dependency.DstSubpass = 0;
			dependency.SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit;
			dependency.SrcAccessMask = 0;
			dependency.DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit;
			dependency.DstAccessMask = AccessFlags.ColorAttachmentWriteBit | AccessFlags.DepthStencilAttachmentWriteBit;

			AttachmentDescription* attachments = stackalloc AttachmentDescription[2];
			attachments[0] = colorAttachment;
			attachments[1] = depthAttatchment;

			RenderPassCreateInfo createInfo = new();
			createInfo.SType = StructureType.RenderPassCreateInfo;
			createInfo.AttachmentCount = 2;
			createInfo.PAttachments = attachments;
			createInfo.SubpassCount = 1;
			createInfo.PSubpasses = &subpass;
			createInfo.DependencyCount = 1;
			createInfo.PDependencies = &dependency;

			var result = context.vk.CreateRenderPass(context.device, createInfo, null, out RenderPass renderPass);
			if (result != Result.Success)
				throw new Exception("Failed to create vkRenderPass");

			return renderPass;
		}

		public static unsafe RenderPass CreateMeshRenderPass(VkContext context, Format colorFormat, Format depthFormat)
		{
			AttachmentDescription colorAttachment = new();
			colorAttachment.Format = colorFormat;
			colorAttachment.Samples = SampleCountFlags.Count1Bit;
			colorAttachment.LoadOp = AttachmentLoadOp.Load;
			colorAttachment.StoreOp = AttachmentStoreOp.Store;
			colorAttachment.StencilLoadOp = AttachmentLoadOp.DontCare;
			colorAttachment.StencilStoreOp = AttachmentStoreOp.DontCare;
			colorAttachment.InitialLayout = ImageLayout.PresentSrcKhr;
			colorAttachment.FinalLayout = ImageLayout.PresentSrcKhr;

			AttachmentDescription depthAttatchment = new();
			depthAttatchment.Format = depthFormat;
			depthAttatchment.Samples = SampleCountFlags.Count1Bit;
			depthAttatchment.LoadOp = AttachmentLoadOp.Clear;
			depthAttatchment.StoreOp = AttachmentStoreOp.Store;
			depthAttatchment.StencilLoadOp = AttachmentLoadOp.DontCare;
			depthAttatchment.StencilStoreOp = AttachmentStoreOp.DontCare;
			depthAttatchment.InitialLayout = ImageLayout.DepthStencilAttachmentOptimal;
			depthAttatchment.FinalLayout = ImageLayout.DepthStencilAttachmentOptimal;

			AttachmentReference colorAttachmentRef = new();
			colorAttachmentRef.Attachment = 0;
			colorAttachmentRef.Layout = ImageLayout.ColorAttachmentOptimal;

			AttachmentReference depthAttachmentRef = new();
			depthAttachmentRef.Attachment = 1;
			depthAttachmentRef.Layout = ImageLayout.DepthStencilAttachmentOptimal;

			SubpassDescription subpass = new();
			subpass.PipelineBindPoint = PipelineBindPoint.Graphics;
			subpass.ColorAttachmentCount = 1;
			subpass.PColorAttachments = &colorAttachmentRef;
			subpass.PDepthStencilAttachment = &depthAttachmentRef;

			SubpassDependency dependency = new();
			dependency.SrcSubpass = Vk.SubpassExternal;
			dependency.DstSubpass = 0;
			dependency.SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit;
			dependency.SrcAccessMask = 0;
			dependency.DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit;
			dependency.DstAccessMask = AccessFlags.ColorAttachmentWriteBit | AccessFlags.DepthStencilAttachmentWriteBit;

			AttachmentDescription* attachments = stackalloc AttachmentDescription[2];
			attachments[0] = colorAttachment;
			attachments[1] = depthAttatchment;

			RenderPassCreateInfo createInfo = new();
			createInfo.SType = StructureType.RenderPassCreateInfo;
			createInfo.AttachmentCount = 2;
			createInfo.PAttachments = attachments;
			createInfo.SubpassCount = 1;
			createInfo.PSubpasses = &subpass;
			createInfo.DependencyCount = 1;
			createInfo.PDependencies = &dependency;

			var result = context.vk.CreateRenderPass(context.device, createInfo, null, out RenderPass renderPass);
			if (result != Result.Success)
				throw new Exception("Failed to create vkRenderPass");

			return renderPass;
		}

		public static unsafe DescriptorSetLayout CreateDescriptorSetLayout(VkContext context)
		{
			DescriptorSetLayoutBinding uniformBinding = new();
			uniformBinding.Binding = 0;
			uniformBinding.DescriptorType = DescriptorType.UniformBuffer;
			uniformBinding.DescriptorCount = 1;
			uniformBinding.StageFlags = ShaderStageFlags.VertexBit;
			uniformBinding.PImmutableSamplers = null;

            DescriptorSetLayoutBinding textureSamplerBinding = new();
            textureSamplerBinding.Binding = 1;
            textureSamplerBinding.DescriptorType = DescriptorType.CombinedImageSampler;
            textureSamplerBinding.DescriptorCount = 1;
            textureSamplerBinding.StageFlags = ShaderStageFlags.FragmentBit;
            textureSamplerBinding.PImmutableSamplers = null;

            DescriptorSetLayoutBinding albedoSamplerBinding = new();
			albedoSamplerBinding.Binding = 2;
			albedoSamplerBinding.DescriptorType = DescriptorType.CombinedImageSampler;
			albedoSamplerBinding.DescriptorCount = 1;
			albedoSamplerBinding.StageFlags = ShaderStageFlags.FragmentBit;
			albedoSamplerBinding.PImmutableSamplers = null;

			DescriptorSetLayoutBinding normalSamplerBinding = new();
			normalSamplerBinding.Binding = 3;
			normalSamplerBinding.DescriptorType = DescriptorType.CombinedImageSampler;
			normalSamplerBinding.DescriptorCount = 1;
			normalSamplerBinding.StageFlags = ShaderStageFlags.FragmentBit;
			normalSamplerBinding.PImmutableSamplers = null;

			DescriptorSetLayoutBinding metallicSamplerBinding = new();
			metallicSamplerBinding.Binding = 4;
			metallicSamplerBinding.DescriptorType = DescriptorType.CombinedImageSampler;
			metallicSamplerBinding.DescriptorCount = 1;
			metallicSamplerBinding.StageFlags = ShaderStageFlags.FragmentBit;
			metallicSamplerBinding.PImmutableSamplers = null;

			DescriptorSetLayoutBinding roughnessSamplerBinding = new();
			roughnessSamplerBinding.Binding = 5;
			roughnessSamplerBinding.DescriptorType = DescriptorType.CombinedImageSampler;
			roughnessSamplerBinding.DescriptorCount = 1;
			roughnessSamplerBinding.StageFlags = ShaderStageFlags.FragmentBit;
			roughnessSamplerBinding.PImmutableSamplers = null;

            /*
			DescriptorSetLayoutBinding aoSamplerBinding = new();
			aoSamplerBinding.Binding = 6;
			aoSamplerBinding.DescriptorType = DescriptorType.CombinedImageSampler;
			aoSamplerBinding.DescriptorCount = 1;
			aoSamplerBinding.StageFlags = ShaderStageFlags.FragmentBit;
			aoSamplerBinding.PImmutableSamplers = null;
			*/

            DescriptorSetLayoutBinding skyboxSamplerBinding = new();
            skyboxSamplerBinding.Binding = 6;
            skyboxSamplerBinding.DescriptorType = DescriptorType.CombinedImageSampler;
            skyboxSamplerBinding.DescriptorCount = 1;
            skyboxSamplerBinding.StageFlags = ShaderStageFlags.FragmentBit;
            skyboxSamplerBinding.PImmutableSamplers = null;

            DescriptorSetLayoutBinding materialBinding = new();
			materialBinding.Binding = 7;
			materialBinding.DescriptorType = DescriptorType.UniformBuffer;
			materialBinding.DescriptorCount = 1;
			materialBinding.StageFlags = ShaderStageFlags.FragmentBit;
			materialBinding.PImmutableSamplers = null;

			DescriptorSetLayoutBinding lightBinding = new();
			lightBinding.Binding = 8;
			lightBinding.DescriptorType = DescriptorType.UniformBuffer;
			lightBinding.DescriptorCount = 4;
			lightBinding.StageFlags = ShaderStageFlags.FragmentBit;
			lightBinding.PImmutableSamplers = null;

			DescriptorSetLayoutCreateInfo createInfo = new();
			createInfo.SType = StructureType.DescriptorSetLayoutCreateInfo;
			createInfo.BindingCount = 9;

			DescriptorSetLayoutBinding* bindingsPtr = stackalloc DescriptorSetLayoutBinding[9];
			bindingsPtr[0] = uniformBinding;
			bindingsPtr[1] = textureSamplerBinding;
			bindingsPtr[2] = albedoSamplerBinding;
			bindingsPtr[3] = normalSamplerBinding;
			bindingsPtr[4] = metallicSamplerBinding;
			bindingsPtr[5] = roughnessSamplerBinding;
			bindingsPtr[6] = skyboxSamplerBinding;
			bindingsPtr[7] = materialBinding;
			bindingsPtr[8] = lightBinding;

			createInfo.PBindings = bindingsPtr;

			var result = context.vk.CreateDescriptorSetLayout(context.device, createInfo, null, out DescriptorSetLayout descriptorSetLayout);
			if (result != Result.Success)
				throw new Exception("Failed to create vkDescriptorSetLayout");

			return descriptorSetLayout;
		}

		// TODO: Make private
		public static unsafe PipelineLayout CreatePipelineLayout(VkContext context, DescriptorSetLayout descriptorSetLayout)
		{
			PipelineColorBlendAttachmentState colorBlendAttatchment = new();
			colorBlendAttatchment.ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit;
			colorBlendAttatchment.BlendEnable = false;
			colorBlendAttatchment.SrcColorBlendFactor = BlendFactor.One;
			colorBlendAttatchment.DstColorBlendFactor = BlendFactor.Zero;
			colorBlendAttatchment.ColorBlendOp = BlendOp.Add;
			colorBlendAttatchment.SrcAlphaBlendFactor = BlendFactor.One;
			colorBlendAttatchment.DstAlphaBlendFactor = BlendFactor.One;
			colorBlendAttatchment.AlphaBlendOp = BlendOp.Add;

			PushConstantRange pushConstant = new();
			pushConstant.Offset = 0;
			pushConstant.Size = (uint)sizeof(PushConstant);
			pushConstant.StageFlags = ShaderStageFlags.VertexBit;

			PipelineLayoutCreateInfo pipelineLayoutCreateInfo = new();
			pipelineLayoutCreateInfo.SType = StructureType.PipelineLayoutCreateInfo;
			pipelineLayoutCreateInfo.SetLayoutCount = 1;
			pipelineLayoutCreateInfo.PSetLayouts = &descriptorSetLayout;
			//pipelineLayoutCreateInfo.PushConstantRangeCount = 1;
			//pipelineLayoutCreateInfo.PPushConstantRanges = &pushConstant;

			var result = context.vk.CreatePipelineLayout(context.device, pipelineLayoutCreateInfo, null, out PipelineLayout pipelineLayout);
			if (result != Result.Success)
				throw new Exception("Failed to create vkPipelineLayout");

			return pipelineLayout;
		}

		static unsafe ShaderModule CreateShaderModule(Vk vk, byte[] code, Device device)
		{
			ShaderModuleCreateInfo createInfo = new();
			createInfo.SType = StructureType.ShaderModuleCreateInfo;
			createInfo.CodeSize = (nuint)code.Length;

			fixed (byte* codePtr = code)
			{
				createInfo.PCode = (uint*)codePtr;
			}

			var result = vk.CreateShaderModule(device, createInfo, null, out ShaderModule shaderModule);
			if (result != Result.Success)
				throw new Exception("Failed to create vkShaderModule");

			return shaderModule;
		}

		static VertexInputBindingDescription GetBindingDescription()
		{
			VertexInputBindingDescription description = new();
			description.Binding = 0;
			description.Stride = Vertex.SizeInBytes;
			description.InputRate = VertexInputRate.Vertex;

			return description;
		}

		static Memory<VertexInputAttributeDescription> GetAttributeDescription()
		{
			Memory<VertexInputAttributeDescription> description = new VertexInputAttributeDescription[3];
			description.Span[0].Binding = 0;
			description.Span[0].Location = 0;
			description.Span[0].Format = Format.R32G32B32Sfloat;
			description.Span[0].Offset = 0;

			description.Span[1].Binding = 0;
			description.Span[1].Location = 1;
			description.Span[1].Format = Format.R32G32B32Sfloat;
			description.Span[1].Offset = sizeof(float) * 3;

			description.Span[2].Binding = 0;
			description.Span[2].Location = 2;
			description.Span[2].Format = Format.R32G32Sfloat;
			description.Span[2].Offset = sizeof(float) * 3 * 2;

			return description;
		}

		// TMP: Make private
		public static unsafe Pipeline CreateGraphicsPipeline(VkContext context, Extent2D swapchainExtent, PipelineLayout pipelineLayout, RenderPass renderPass, Shader shader)
		{
			//var shader = Shader.FromFiles("Shaders/VulkanVert.spv", "Shaders/VulkanFrag.spv");
			//var shader = Shader.FromFiles("Shaders/Pbr/PbrVert.spv", "Shaders/Pbr/PbrFrag.spv");

			var vertShader = CreateShaderModule(context.vk, shader.Vertex, context.device);
			var fragShader = CreateShaderModule(context.vk, shader.Fragment, context.device);

			PipelineShaderStageCreateInfo vertexStageCreateInfo = new();
			vertexStageCreateInfo.SType = StructureType.PipelineShaderStageCreateInfo;
			vertexStageCreateInfo.Stage = ShaderStageFlags.VertexBit;

			vertexStageCreateInfo.Module = vertShader;
			vertexStageCreateInfo.PName = (byte*)Marshal.StringToHGlobalAnsi("main");

			PipelineShaderStageCreateInfo fragmentStageCreateInfo = new();
			fragmentStageCreateInfo.SType = StructureType.PipelineShaderStageCreateInfo;
			fragmentStageCreateInfo.Stage = ShaderStageFlags.FragmentBit;

			fragmentStageCreateInfo.Module = fragShader;
			fragmentStageCreateInfo.PName = (byte*)Marshal.StringToHGlobalAnsi("main");

			Span<PipelineShaderStageCreateInfo> shaderStages = [vertexStageCreateInfo, fragmentStageCreateInfo];

			DynamicState* dynamicState = stackalloc DynamicState[2] { DynamicState.Viewport, DynamicState.Scissor };

			PipelineDynamicStateCreateInfo dynamicStateCreateInfo = new();
			dynamicStateCreateInfo.SType = StructureType.PipelineDynamicStateCreateInfo;
			dynamicStateCreateInfo.DynamicStateCount = 2;
			dynamicStateCreateInfo.PDynamicStates = &dynamicState[0];

			VertexInputBindingDescription bindingDescription = GetBindingDescription();
			Memory<VertexInputAttributeDescription> attributeDescription = GetAttributeDescription();

			PipelineVertexInputStateCreateInfo vertexInputStateCreateInfo = new();
			vertexInputStateCreateInfo.SType = StructureType.PipelineVertexInputStateCreateInfo;
			vertexInputStateCreateInfo.VertexBindingDescriptionCount = 1;
			vertexInputStateCreateInfo.PVertexBindingDescriptions = &bindingDescription;
			vertexInputStateCreateInfo.VertexAttributeDescriptionCount = (uint)attributeDescription.Length;
			fixed (VertexInputAttributeDescription* attributeDescriptionPtr = attributeDescription.Span)
			{
				vertexInputStateCreateInfo.PVertexAttributeDescriptions = attributeDescriptionPtr;
			}

			PipelineInputAssemblyStateCreateInfo inputAssemblyStateCreateInfo = new();
			inputAssemblyStateCreateInfo.SType = StructureType.PipelineInputAssemblyStateCreateInfo;
			inputAssemblyStateCreateInfo.Topology = PrimitiveTopology.TriangleList;
			inputAssemblyStateCreateInfo.PrimitiveRestartEnable = false;

			Viewport viewport = new();
			viewport.X = 0;
			viewport.Y = 0;
			viewport.Width = swapchainExtent.Width;
			viewport.Height = swapchainExtent.Height;
			viewport.MinDepth = 0;
			viewport.MaxDepth = 1;

			Rect2D scissor = new();
			scissor.Offset = new(0, 0);
			scissor.Extent = swapchainExtent;

			PipelineViewportStateCreateInfo viewportStateCreateInfo = new();
			viewportStateCreateInfo.SType = StructureType.PipelineViewportStateCreateInfo;
			viewportStateCreateInfo.ViewportCount = 1;
			viewportStateCreateInfo.PViewports = &viewport;
			viewportStateCreateInfo.ScissorCount = 1;
			viewportStateCreateInfo.PScissors = &scissor;

			PipelineRasterizationStateCreateInfo rasterizationStateCreateInfo = new();
			rasterizationStateCreateInfo.SType = StructureType.PipelineRasterizationStateCreateInfo;
			rasterizationStateCreateInfo.DepthClampEnable = false;
			rasterizationStateCreateInfo.RasterizerDiscardEnable = false;
			rasterizationStateCreateInfo.PolygonMode = PolygonMode.Fill;
			rasterizationStateCreateInfo.LineWidth = 1.0f;
			rasterizationStateCreateInfo.CullMode = CullModeFlags.None;
			rasterizationStateCreateInfo.FrontFace = FrontFace.CounterClockwise;
			rasterizationStateCreateInfo.DepthBiasEnable = false;
			rasterizationStateCreateInfo.DepthBiasConstantFactor = 0;
			rasterizationStateCreateInfo.DepthBiasClamp = 0;
			rasterizationStateCreateInfo.DepthBiasSlopeFactor = 0;

			PipelineMultisampleStateCreateInfo multisampleStateCreateInfo = new();
			multisampleStateCreateInfo.SType = StructureType.PipelineMultisampleStateCreateInfo;
			multisampleStateCreateInfo.SampleShadingEnable = false;
			multisampleStateCreateInfo.RasterizationSamples = SampleCountFlags.Count1Bit;
			multisampleStateCreateInfo.MinSampleShading = 1;
			multisampleStateCreateInfo.PSampleMask = null;
			multisampleStateCreateInfo.AlphaToCoverageEnable = false;
			multisampleStateCreateInfo.AlphaToOneEnable = false;

			PipelineColorBlendAttachmentState colorBlendAttatchment = new();
			colorBlendAttatchment.ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit;
			colorBlendAttatchment.BlendEnable = false;
			colorBlendAttatchment.SrcColorBlendFactor = BlendFactor.One;
			colorBlendAttatchment.DstColorBlendFactor = BlendFactor.Zero;
			colorBlendAttatchment.ColorBlendOp = BlendOp.Add;
			colorBlendAttatchment.SrcAlphaBlendFactor = BlendFactor.One;
			colorBlendAttatchment.DstAlphaBlendFactor = BlendFactor.One;
			colorBlendAttatchment.AlphaBlendOp = BlendOp.Add;

			PipelineColorBlendStateCreateInfo colorBlendStateCreateInfo = new();
			colorBlendStateCreateInfo.SType = StructureType.PipelineColorBlendStateCreateInfo;
			colorBlendStateCreateInfo.LogicOpEnable = false;
			colorBlendStateCreateInfo.LogicOp = LogicOp.Copy;
			colorBlendStateCreateInfo.AttachmentCount = 1;
			colorBlendStateCreateInfo.PAttachments = &colorBlendAttatchment;
			colorBlendStateCreateInfo.BlendConstants[0] = 0;
			colorBlendStateCreateInfo.BlendConstants[1] = 0;
			colorBlendStateCreateInfo.BlendConstants[2] = 0;
			colorBlendStateCreateInfo.BlendConstants[3] = 0;

			PipelineDepthStencilStateCreateInfo depthStencilStateCreateInfo = new();
			depthStencilStateCreateInfo.SType = StructureType.PipelineDepthStencilStateCreateInfo;
			depthStencilStateCreateInfo.DepthTestEnable = true;
			depthStencilStateCreateInfo.DepthWriteEnable = true;
			depthStencilStateCreateInfo.DepthCompareOp = CompareOp.Less;
			depthStencilStateCreateInfo.DepthBoundsTestEnable = false;
			depthStencilStateCreateInfo.MinDepthBounds = 0.0f;
			depthStencilStateCreateInfo.MaxDepthBounds = 1.0f;
			depthStencilStateCreateInfo.StencilTestEnable = false;
			depthStencilStateCreateInfo.Front = default;
			depthStencilStateCreateInfo.Back = default;

			GraphicsPipelineCreateInfo graphicsCreateInfo = new();
			graphicsCreateInfo.SType = StructureType.GraphicsPipelineCreateInfo;
			graphicsCreateInfo.StageCount = (uint)shaderStages.Length;
			fixed (PipelineShaderStageCreateInfo* shaderStagesPtr = shaderStages)
			{
				graphicsCreateInfo.PStages = shaderStagesPtr;
			}

			graphicsCreateInfo.PVertexInputState = &vertexInputStateCreateInfo;
			graphicsCreateInfo.PInputAssemblyState = &inputAssemblyStateCreateInfo;
			graphicsCreateInfo.PViewportState = &viewportStateCreateInfo;
			graphicsCreateInfo.PRasterizationState = &rasterizationStateCreateInfo;
			graphicsCreateInfo.PMultisampleState = &multisampleStateCreateInfo;
			graphicsCreateInfo.PDepthStencilState = null;
			graphicsCreateInfo.PColorBlendState = &colorBlendStateCreateInfo;
			graphicsCreateInfo.PDynamicState = &dynamicStateCreateInfo;
			graphicsCreateInfo.Layout = pipelineLayout;
			graphicsCreateInfo.RenderPass = renderPass;
			graphicsCreateInfo.Subpass = 0;
			graphicsCreateInfo.BasePipelineHandle = default;
			graphicsCreateInfo.BasePipelineIndex = -1;
			graphicsCreateInfo.PDepthStencilState = &depthStencilStateCreateInfo;

			var result = context.vk.CreateGraphicsPipelines(context.device, default, 1, graphicsCreateInfo, null, out Pipeline pipeline);
			if (result != Result.Success)
				throw new Exception("Failed to create vkPipelineLayout");

			context.vk.DestroyShaderModule(context.device, vertShader, null);
			context.vk.DestroyShaderModule(context.device, fragShader, null);

			return pipeline;
		}
	}
}
