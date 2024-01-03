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

namespace Engine
{
	public struct RenderPipeline
	{
		public struct FrameData
		{
			public Semaphore imageAvailable;
			public Semaphore renderFinished;
			public Fence inFlight;

			public DescriptorSet descriptorSet;
			//public FixedArray8<DescriptorSet> descriptorSets;
			public CommandBuffer commandBuffer;
			public MappedMemory<UniformBufferObject> uboMemory;
		}

		struct PushConstant
		{
			public Matrix4x4 model;
		}

		const int MAX_FRAMES_IN_FLIGHT = 3;

		Swapchain swapchain;
		RenderPass renderPass;
		Sampler sampler;
		DescriptorSetLayout descriptorSetLayout;
		PipelineLayout pipelineLayout;
		Pipeline pipeline;

		Memory<ImageView> swapchainImages;
		Memory<Framebuffer> frameBuffers;
		Memory<FrameData> framesInFlight;

		int currentFrame;
		uint imageIndex;

		public RenderPipeline(Swapchain swapchain, RenderPass renderPass, Sampler sampler, DescriptorSetLayout descriptorSetLayout, PipelineLayout pipelineLayout, Pipeline pipeline, Memory<ImageView> swapchainImages, Memory<Framebuffer> frameBuffers, Memory<FrameData> framesInFlight) : this()
		{
			this.swapchain = swapchain;
			this.renderPass = renderPass;
			this.sampler = sampler;
			this.descriptorSetLayout = descriptorSetLayout;
			this.pipelineLayout = pipelineLayout;
			this.pipeline = pipeline;
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

			context.vk.DestroyPipeline(context.device, pipeline, null);
			context.vk.DestroyPipelineLayout(context.device, pipelineLayout, null);
			context.vk.DestroyRenderPass(context.device, renderPass, null);
		}

		public void RecreateSwapchain(VkContext context, SurfaceKHR surface, CommandPool commandPool)
		{
			/*
			 Vector2D<int> framebufferSize = window!.FramebufferSize;

			while (framebufferSize.X == 0 || framebufferSize.Y == 0)
			{
				framebufferSize = window.FramebufferSize;
				window.DoEvents();
			}
			 */

			context.vk.DeviceWaitIdle(context.device);

			swapchain.Dispose(context);
			DisposeSwapchain(context, commandPool);

			swapchain = Swapchain.Create(context, surface, commandPool);
			renderPass = CreateRenderPass(context, swapchain, Swapchain.GetDepthFormat(context));
			pipelineLayout = CreatePipelineLayout(context, descriptorSetLayout);
			pipeline = CreateGraphicsPipeline(context, swapchain.GetExtent(), pipelineLayout, renderPass);

			swapchain.GetImages(context, swapchainImages.Span);
			for (int i = 0; i < frameBuffers.Span.Length; i++)
			{
				frameBuffers.Span[i] = VulkanHelper.CreateFrameBuffer(context, [swapchainImages.Span[i], swapchain.GetDepthImage()], renderPass, swapchain.GetExtent());
			}

			for (int i = 0; i < framesInFlight.Span.Length; i++)
			{
				framesInFlight.Span[i].commandBuffer = VulkanHelper.CreateCommandBuffer(context, commandPool);
			}
		}

		public unsafe Result StartRender(VkContext context)
		{
			ref FrameData frame = ref framesInFlight.Span[currentFrame];
			VulkanHelper.WaitForFence(context, frame.inFlight);

			var aquireResult = swapchain.AcquireNextImageIndex(context, frame.imageAvailable, out imageIndex);

			if (aquireResult == Result.ErrorOutOfDateKhr)
				return aquireResult;

			VulkanHelper.WaitForFence(context, frame.inFlight);
			context.vk.ResetCommandBuffer(frame.commandBuffer, CommandBufferResetFlags.ReleaseResourcesBit);

			System.Drawing.Color color = System.Drawing.Color.CornflowerBlue;
			ClearColorValue clearColor = new() { Float32_0 = color.R / 255f, Float32_1 = color.G / 255f, Float32_2 = color.B / 255f, Float32_3 = color.A / 255f };

			var renderArea = new Rect2D(new(), swapchain.GetExtent());

			Framebuffer framebuffer = frameBuffers.Span[(int)imageIndex];
			BeginRenderCommand(context, frame.commandBuffer, framebuffer, clearColor, renderArea);
			RenderSetViewportAndScissor(context, frame.commandBuffer, renderArea);

			context.vk.CmdBindDescriptorSets(frame.commandBuffer, PipelineBindPoint.Graphics, pipelineLayout, 0, 1, frame.descriptorSet, 0, null);

			return aquireResult;
		}

		public unsafe void Render(VkContext context, ref UniformBufferObject ubo, Buffer vertexBuffer, Buffer indexBuffer, uint indicies, ImageView texture)
		{
			ref FrameData frame = ref framesInFlight.Span[currentFrame];
			frame.uboMemory.Value = ubo;

			/*
			{
				DescriptorImageInfo imageInfo = new();
				imageInfo.ImageLayout = ImageLayout.ReadOnlyOptimal;
				imageInfo.ImageView = texture;
				imageInfo.Sampler = sampler;

				WriteDescriptorSet imageDescriptorWrite = new();
				imageDescriptorWrite.SType = StructureType.WriteDescriptorSet;
				imageDescriptorWrite.DstSet = frame.descriptorSet;
				imageDescriptorWrite.DstBinding = 1;
				imageDescriptorWrite.DstArrayElement = 0;
				imageDescriptorWrite.DescriptorType = DescriptorType.CombinedImageSampler;
				imageDescriptorWrite.DescriptorCount = 1;
				imageDescriptorWrite.PImageInfo = &imageInfo;

				context.vk.UpdateDescriptorSets(context.device, [imageDescriptorWrite], 0, null);
			}
			*/

			var pc = new PushConstant()
			{
				model = ubo.scale * ubo.rotation * ubo.translation
			};

			context.vk.CmdBindPipeline(frame.commandBuffer, PipelineBindPoint.Graphics, pipeline);

			context.vk.CmdBindVertexBuffers(frame.commandBuffer, 0, [vertexBuffer], [0]);
			context.vk.CmdBindIndexBuffer(frame.commandBuffer, indexBuffer, 0, IndexType.Uint16);

			context.vk.CmdPushConstants(frame.commandBuffer, pipelineLayout, ShaderStageFlags.VertexBit, 0, (uint)sizeof(PushConstant), ref pc);

			context.vk.CmdDrawIndexed(frame.commandBuffer, indicies, 1, 0, 0, 0);
		}

		public void EndRender(VkContext context)
		{
            ref FrameData frame = ref framesInFlight.Span[currentFrame];

			FinishRender(context, frame.commandBuffer);
			var result = context.vk.ResetFences(context.device, [frame.inFlight]);

            VulkanHelper.QueueSubmitCommands(context, swapchain.GetGraphicsQueue(), frame.commandBuffer, frame.imageAvailable, frame.inFlight, PipelineStageFlags.ColorAttachmentOutputBit);
			VulkanHelper.QueuePresent(context, swapchain.GetPresentQueue(), swapchain.GetSwapchain(), imageIndex, frame.imageAvailable);

			// TODO: Improve
			currentFrame = (currentFrame + 1) % MAX_FRAMES_IN_FLIGHT;
		}

		unsafe void BeginRenderCommand(VkContext context, CommandBuffer commandBuffer, Framebuffer framebuffer, ClearColorValue clearColor, Rect2D renderArea)
		{
			CommandBufferBeginInfo beginInfo = new();
			beginInfo.SType = StructureType.CommandBufferBeginInfo;
			beginInfo.Flags = 0;
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

		public static RenderPipeline Create(VkContext context, SurfaceKHR surface, ImageView image, CommandPool commandPool)
		{
			Swapchain swapchain = Swapchain.Create(context, surface, commandPool);

			RenderPass renderPass = CreateRenderPass(context, swapchain, Swapchain.GetDepthFormat(context));

			Sampler sampler = VulkanHelper.CreateSampler(context);

			DescriptorSetLayout descriptorSetLayout = CreateDescriptorSetLayout(context);
			PipelineLayout pipelineLayout = CreatePipelineLayout(context, descriptorSetLayout);
			Pipeline pipeline = CreateGraphicsPipeline(context, swapchain.GetExtent(), pipelineLayout, renderPass);

			Memory<ImageView> swapchainImages = new ImageView[swapchain.GetImageCount()];
			swapchain.GetImages(context, swapchainImages.Span);

			Memory<Framebuffer> frameBuffers = new Framebuffer[swapchain.GetImageCount()];
			for (int i = 0; i < frameBuffers.Span.Length; i++)
			{
				frameBuffers.Span[i] = VulkanHelper.CreateFrameBuffer(context, [swapchainImages.Span[i], swapchain.GetDepthImage()], renderPass, swapchain.GetExtent());
			}

			DescriptorPool descriptorPool = VulkanHelper.CreateDescriptorPool(context, MAX_FRAMES_IN_FLIGHT * 8);
			Memory<FrameData> framesInFlight = CreateFramesInFlight(context, descriptorPool, commandPool, descriptorSetLayout, sampler, image);

			return new RenderPipeline(swapchain, renderPass, sampler, descriptorSetLayout, pipelineLayout, pipeline, swapchainImages, frameBuffers, framesInFlight);
		}

		static unsafe Memory<FrameData> CreateFramesInFlight(VkContext context, DescriptorPool descriptorPool, CommandPool commandPool, DescriptorSetLayout layout, Sampler sampler, ImageView image)
		{
			Memory<FrameData> framesInFlight = new FrameData[MAX_FRAMES_IN_FLIGHT];
			for (int i = 0; i < framesInFlight.Length; i++)
			{
				framesInFlight.Span[i].imageAvailable = VulkanHelper.CreateSemaphore(context);
				framesInFlight.Span[i].renderFinished = VulkanHelper.CreateSemaphore(context);
				framesInFlight.Span[i].inFlight = VulkanHelper.CreateFence(context, FenceCreateFlags.SignaledBit);
				framesInFlight.Span[i].commandBuffer = VulkanHelper.CreateCommandBuffer(context, commandPool);

				Buffer uniformBuffer = VulkanHelper.CreateBuffer<UniformBufferObject>(context, BufferUsageFlags.UniformBufferBit, 1);
				DeviceMemory uniformBuffersMemory = VulkanHelper.CreateBufferMemory(context, uniformBuffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

				void* dataPtr;
				context.vk.MapMemory(context.device, uniformBuffersMemory, 0, (ulong)sizeof(UniformBufferObject), 0, &dataPtr);
				framesInFlight.Span[i].uboMemory = new((UniformBufferObject*)dataPtr);

				DescriptorSetAllocateInfo allocInfo = new();
				allocInfo.SType = StructureType.DescriptorSetAllocateInfo;
				allocInfo.DescriptorPool = descriptorPool;
				allocInfo.DescriptorSetCount = 1;
				allocInfo.PSetLayouts = &layout;

				var result = context.vk.AllocateDescriptorSets(context.device, allocInfo, out DescriptorSet descriptorSet);
				if (result != Result.Success)
					throw new Exception("Failed to allocate vkDescriptorSets");

				DescriptorBufferInfo bufferInfo = new();
				bufferInfo.Buffer = uniformBuffer;
				bufferInfo.Offset = 0;
				bufferInfo.Range = (ulong)sizeof(UniformBufferObject);

				DescriptorImageInfo imageInfo = new();
				imageInfo.ImageLayout = ImageLayout.ReadOnlyOptimal;
				imageInfo.ImageView = image;
				imageInfo.Sampler = sampler;

				WriteDescriptorSet bufferDescriptorWrite = new();
				bufferDescriptorWrite.SType = StructureType.WriteDescriptorSet;
				bufferDescriptorWrite.DstSet = descriptorSet;
				bufferDescriptorWrite.DstBinding = 0;
				bufferDescriptorWrite.DstArrayElement = 0;
				bufferDescriptorWrite.DescriptorType = DescriptorType.UniformBuffer;
				bufferDescriptorWrite.DescriptorCount = 1;
				bufferDescriptorWrite.PBufferInfo = &bufferInfo;

				WriteDescriptorSet imageDescriptorWrite = new();
				imageDescriptorWrite.SType = StructureType.WriteDescriptorSet;
				imageDescriptorWrite.DstSet = descriptorSet;
				imageDescriptorWrite.DstBinding = 1;
				imageDescriptorWrite.DstArrayElement = 0;
				imageDescriptorWrite.DescriptorType = DescriptorType.CombinedImageSampler;
				imageDescriptorWrite.DescriptorCount = 1;
				imageDescriptorWrite.PImageInfo = &imageInfo;

				context.vk.UpdateDescriptorSets(context.device, [bufferDescriptorWrite, imageDescriptorWrite], 0, null);

				framesInFlight.Span[i].descriptorSet = descriptorSet;
			}

			/*
			Buffer uniformBuffer = VulkanHelper.CreateBuffer<UniformBufferObject>(context, BufferUsageFlags.UniformBufferBit, 1);
			DeviceMemory uniformBuffersMemory = VulkanHelper.CreateBufferMemory(context, uniformBuffer, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

			void* dataPtr;
			context.vk.MapMemory(context.device, uniformBuffersMemory, 0, (ulong)sizeof(UniformBufferObject), 0, &dataPtr);
			framesInFlight.Span[0].uboMemory = new((UniformBufferObject*)dataPtr);
			framesInFlight.Span[1].uboMemory = new((UniformBufferObject*)dataPtr);
			framesInFlight.Span[2].uboMemory = new((UniformBufferObject*)dataPtr);

			DescriptorSetAllocateInfo allocInfo = new();
			allocInfo.SType = StructureType.DescriptorSetAllocateInfo;
			allocInfo.DescriptorPool = descriptorPool;
			allocInfo.DescriptorSetCount = 1;
			allocInfo.PSetLayouts = &layout;

			var result = context.vk.AllocateDescriptorSets(context.device, allocInfo, out DescriptorSet descriptorSet);
			if (result != Result.Success)
				throw new Exception("Failed to allocate vkDescriptorSets");

			DescriptorBufferInfo bufferInfo = new();
			bufferInfo.Buffer = uniformBuffer;
			bufferInfo.Offset = 0;
			bufferInfo.Range = (ulong)sizeof(UniformBufferObject);

			DescriptorImageInfo imageInfo = new();
			imageInfo.ImageLayout = ImageLayout.ReadOnlyOptimal;
			imageInfo.ImageView = image;
			imageInfo.Sampler = sampler;

			WriteDescriptorSet bufferDescriptorWrite = new();
			bufferDescriptorWrite.SType = StructureType.WriteDescriptorSet;
			bufferDescriptorWrite.DstSet = descriptorSet;
			bufferDescriptorWrite.DstBinding = 0;
			bufferDescriptorWrite.DstArrayElement = 0;
			bufferDescriptorWrite.DescriptorType = DescriptorType.UniformBuffer;
			bufferDescriptorWrite.DescriptorCount = 1;
			bufferDescriptorWrite.PBufferInfo = &bufferInfo;

			WriteDescriptorSet imageDescriptorWrite = new();
			imageDescriptorWrite.SType = StructureType.WriteDescriptorSet;
			imageDescriptorWrite.DstSet = descriptorSet;
			imageDescriptorWrite.DstBinding = 1;
			imageDescriptorWrite.DstArrayElement = 0;
			imageDescriptorWrite.DescriptorType = DescriptorType.CombinedImageSampler;
			imageDescriptorWrite.DescriptorCount = 1;
			imageDescriptorWrite.PImageInfo = &imageInfo;

			context.vk.UpdateDescriptorSets(context.device, [bufferDescriptorWrite, imageDescriptorWrite], 0, null);

			framesInFlight.Span[0].descriptorSet = descriptorSet;
			framesInFlight.Span[1].descriptorSet = descriptorSet;
			framesInFlight.Span[2].descriptorSet = descriptorSet;
			*/

			return framesInFlight;
		}

		static unsafe RenderPass CreateRenderPass(VkContext context, Swapchain swapchain, Format depthFormat)
		{
			var surfaceFormat = swapchain.GetSurfaceFormat();

			AttachmentDescription colorAttachment = new();
			colorAttachment.Format = surfaceFormat.Format;
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
			depthAttatchment.StoreOp = AttachmentStoreOp.DontCare;
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

		static unsafe DescriptorSetLayout CreateDescriptorSetLayout(VkContext context)
		{
			DescriptorSetLayoutBinding uniformBinding = new();
			uniformBinding.Binding = 0;
			uniformBinding.DescriptorType = DescriptorType.UniformBuffer;
			uniformBinding.DescriptorCount = 1;
			uniformBinding.StageFlags = ShaderStageFlags.VertexBit;
			uniformBinding.PImmutableSamplers = null;

			DescriptorSetLayoutBinding samplerBinding = new();
			samplerBinding.Binding = 1;
			samplerBinding.DescriptorType = DescriptorType.CombinedImageSampler;
			samplerBinding.DescriptorCount = 1;
			samplerBinding.StageFlags = ShaderStageFlags.FragmentBit;
			samplerBinding.PImmutableSamplers = null;

			DescriptorSetLayoutCreateInfo createInfo = new();
			createInfo.SType = StructureType.DescriptorSetLayoutCreateInfo;
			createInfo.BindingCount = 2;

			DescriptorSetLayoutBinding* bindingsPtr = stackalloc DescriptorSetLayoutBinding[2];
			bindingsPtr[0] = uniformBinding;
			bindingsPtr[1] = samplerBinding;

			createInfo.PBindings = bindingsPtr;

			var result = context.vk.CreateDescriptorSetLayout(context.device, createInfo, null, out DescriptorSetLayout descriptorSetLayout);
			if (result != Result.Success)
				throw new Exception("Failed to create vkDescriptorSetLayout");

			return descriptorSetLayout;
		}

		static unsafe PipelineLayout CreatePipelineLayout(VkContext context, DescriptorSetLayout descriptorSetLayout)
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
			pipelineLayoutCreateInfo.PushConstantRangeCount = 1;
			pipelineLayoutCreateInfo.PPushConstantRanges = &pushConstant;

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

		static unsafe Pipeline CreateGraphicsPipeline(VkContext context, Extent2D swapchainExtent, PipelineLayout pipelineLayout, RenderPass renderPass)
		{
			var shader = Shader.FromFiles("Shaders/VulkanVert.spv", "Shaders/VulkanFrag.spv");

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
