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
			skyboxRenderPass = CreateSkyboxRenderPass(context, swapchain, Swapchain.GetDepthFormat(context));
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
            imageDescriptorWrite.DstBinding = 1;
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
			/*
			*/

            FinishRender(context, frame.commandBuffer);
			var result = context.vk.ResetFences(context.device, [frame.inFlight]);

			VulkanHelper.QueueSubmitCommands(context, swapchain.GetGraphicsQueue(), frame.commandBuffer, frame.imageAvailable, frame.inFlight, PipelineStageFlags.ColorAttachmentOutputBit);
            //VulkanHelper.QueuePresent(context, swapchain.GetPresentQueue(), swapchain.GetSwapchain(), imageIndex, frame.imageAvailable);

            return aquireResult;
		}

		unsafe void RenderSkybox(VkContext context)
		{
            
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

		public unsafe void UpdateFrameDescriptorSet(VkContext context, ImageView texture, int idx, VkTextureBuffer albedo, VkTextureBuffer normal, VkTextureBuffer metallic, VkTextureBuffer roughness)
		{
            ref FrameData frame = ref framesInFlight.Span[currentFrame];

			Span<DescriptorImageInfo> infos = stackalloc DescriptorImageInfo[5];
			Span<WriteDescriptorSet> descriptorWrites = stackalloc WriteDescriptorSet[5];

			{
				//DescriptorImageInfo imageInfo = new();
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
				//context.vk.UpdateDescriptorSets(context.device, [imageDescriptorWrite], 0, null);
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
				//context.vk.UpdateDescriptorSets(context.device, [imageDescriptorWrite], 0, null);
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
				//context.vk.UpdateDescriptorSets(context.device, [imageDescriptorWrite], 0, null);
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
				//context.vk.UpdateDescriptorSets(context.device, [imageDescriptorWrite], 0, null);
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
				//context.vk.UpdateDescriptorSets(context.device, [imageDescriptorWrite], 0, null);
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

			RenderPass renderPass = CreateSkyboxRenderPass(context, swapchain, Swapchain.GetDepthFormat(context));
			RenderPass meshRenderPass = CreateMeshRenderPass(context, swapchain, Swapchain.GetDepthFormat(context));

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
						.Variable<PbrMaterial>(6)
						.Array<Light>(7, 4);

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

		static unsafe RenderPass CreateSkyboxRenderPass(VkContext context, Swapchain swapchain, Format depthFormat)
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

		static unsafe RenderPass CreateMeshRenderPass(VkContext context, Swapchain swapchain, Format depthFormat)
		{
			var surfaceFormat = swapchain.GetSurfaceFormat();

			AttachmentDescription colorAttachment = new();
			colorAttachment.Format = surfaceFormat.Format;
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

		static unsafe DescriptorSetLayout CreateDescriptorSetLayout(VkContext context)
		{
			DescriptorSetLayoutBinding uniformBinding = new();
			uniformBinding.Binding = 0;
			uniformBinding.DescriptorType = DescriptorType.UniformBuffer;
			uniformBinding.DescriptorCount = 1;
			uniformBinding.StageFlags = ShaderStageFlags.VertexBit;
			uniformBinding.PImmutableSamplers = null;

			DescriptorSetLayoutBinding albedoSamplerBinding = new();
			albedoSamplerBinding.Binding = 1;
			albedoSamplerBinding.DescriptorType = DescriptorType.CombinedImageSampler;
			albedoSamplerBinding.DescriptorCount = 1;
			albedoSamplerBinding.StageFlags = ShaderStageFlags.FragmentBit;
			albedoSamplerBinding.PImmutableSamplers = null;

			DescriptorSetLayoutBinding normalSamplerBinding = new();
			normalSamplerBinding.Binding = 2;
			normalSamplerBinding.DescriptorType = DescriptorType.CombinedImageSampler;
			normalSamplerBinding.DescriptorCount = 1;
			normalSamplerBinding.StageFlags = ShaderStageFlags.FragmentBit;
			normalSamplerBinding.PImmutableSamplers = null;

			DescriptorSetLayoutBinding metallicSamplerBinding = new();
			metallicSamplerBinding.Binding = 3;
			metallicSamplerBinding.DescriptorType = DescriptorType.CombinedImageSampler;
			metallicSamplerBinding.DescriptorCount = 1;
			metallicSamplerBinding.StageFlags = ShaderStageFlags.FragmentBit;
			metallicSamplerBinding.PImmutableSamplers = null;

			DescriptorSetLayoutBinding roughnessSamplerBinding = new();
			roughnessSamplerBinding.Binding = 4;
			roughnessSamplerBinding.DescriptorType = DescriptorType.CombinedImageSampler;
			roughnessSamplerBinding.DescriptorCount = 1;
			roughnessSamplerBinding.StageFlags = ShaderStageFlags.FragmentBit;
			roughnessSamplerBinding.PImmutableSamplers = null;

			DescriptorSetLayoutBinding aoSamplerBinding = new();
			aoSamplerBinding.Binding = 5;
			aoSamplerBinding.DescriptorType = DescriptorType.CombinedImageSampler;
			aoSamplerBinding.DescriptorCount = 1;
			aoSamplerBinding.StageFlags = ShaderStageFlags.FragmentBit;
			aoSamplerBinding.PImmutableSamplers = null;

			DescriptorSetLayoutBinding materialBinding = new();
			materialBinding.Binding = 6;
			materialBinding.DescriptorType = DescriptorType.UniformBuffer;
			materialBinding.DescriptorCount = 1;
			materialBinding.StageFlags = ShaderStageFlags.FragmentBit;
			materialBinding.PImmutableSamplers = null;

			DescriptorSetLayoutBinding lightBinding = new();
			lightBinding.Binding = 7;
			lightBinding.DescriptorType = DescriptorType.UniformBuffer;
			lightBinding.DescriptorCount = 4;
			lightBinding.StageFlags = ShaderStageFlags.FragmentBit;
			lightBinding.PImmutableSamplers = null;

			DescriptorSetLayoutCreateInfo createInfo = new();
			createInfo.SType = StructureType.DescriptorSetLayoutCreateInfo;
			createInfo.BindingCount = 8;

			DescriptorSetLayoutBinding* bindingsPtr = stackalloc DescriptorSetLayoutBinding[8];
			bindingsPtr[0] = uniformBinding;
			bindingsPtr[1] = albedoSamplerBinding;
			bindingsPtr[2] = normalSamplerBinding;
			bindingsPtr[3] = metallicSamplerBinding;
			bindingsPtr[4] = roughnessSamplerBinding;
			bindingsPtr[5] = aoSamplerBinding;
			bindingsPtr[6] = materialBinding;
			bindingsPtr[7] = lightBinding;

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

		static unsafe Pipeline CreateGraphicsPipeline(VkContext context, Extent2D swapchainExtent, PipelineLayout pipelineLayout, RenderPass renderPass, Shader shader)
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
