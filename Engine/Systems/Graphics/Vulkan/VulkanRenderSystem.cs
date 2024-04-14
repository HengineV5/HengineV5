using EnCS;
using EnCS.Attributes;
using Engine.Components;
using Engine.Graphics;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using SixLabors.ImageSharp;
using System.Buffers;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Engine
{
	public unsafe struct MappedMemory<T> where T : unmanaged
	{
		public ref T Value
		{
			get
			{
				return ref data[0];
			}
		}

		T* data;

        public MappedMemory(T* data)
        {
			this.data = data;
        }
    }

	public struct VulkanRenderContext
	{
		public PbrUniformBufferObject pbrUbo;
		public GuiUniformBufferObject guiUbo;
		public VkSkybox skybox;

        public VulkanRenderContext()
        {
			this.pbrUbo = new PbrUniformBufferObject();
        }
    }

	// To reduce code generation
	/*
	[System<VulkanRenderContext>]
	[UsingResource<VulkanSkyboxResourceManager>]
	[UsingResource<VulkanMeshResourceManager>]
	[UsingResource<VulkanMaterialResourceManager>]
	*/
	public partial class VulkanRenderSystem
	{
		private static FixedArray4<Light> defaultLights = new FixedArray4<Light>();

		private static readonly Light defaultLight = new Light
		{
			Ambient = new Vector3(1f, 1f, 1f),
			Diffuse = new Vector3(0.5f, 0.5f, 0.5f),
			Specular = new Vector3(1f, 1f, 1f)
		};

		IWindow window;
		VkContext context;
		VkRenderContext renderContext;

		bool framebufferResized = false;

        VkMeshBuffer skyboxBuffer;
		VkTextureBuffer skyboxHdrTextureBuffer;

		public VulkanRenderSystem(VkContext context, VkRenderContext renderContext, IWindow window)
		{
			this.context = context;
			this.renderContext = renderContext;
			this.window = window;

			defaultLights[0] = defaultLight;
			defaultLights[1] = defaultLight;
			defaultLights[2] = defaultLight;
			defaultLights[3] = defaultLight;

			defaultLights[0].Position = new Vector3(0, 3, -2);
			defaultLights[1].Position = new Vector3(10, 0, -2);
			defaultLights[2].Position = new Vector3(-10, 0, -2);
			defaultLights[3].Position = new Vector3(0, 10, -2);
        }

		public void Init()
		{
			var skyboxMesh = Mesh.LoadOBJ("Skybox", "Models/Skybox.obj");
			skyboxBuffer = VulkanMeshResourceManager.CreateMeshBuffer(context, skyboxMesh);

			var textureBrdfLUT = ETextureHdr.LoadImage("BrdfLUT", "Images/Skybox/integrationMap.png");
            skyboxHdrTextureBuffer = VulkanTextureResourceManager.CreateHdrTextureBuffer(context, textureBrdfLUT);

            window.FramebufferResize += Window_Resize;
		}

		private void Window_Resize(Vector2D<int> obj)
		{
			framebufferResized = true;
		}

		public unsafe void Dispose()
		{
			//renderContext.renderPipeline.Dispose(context);
		}

		public unsafe void PreRun()
		{
            window.DoEvents();

            Thread.Sleep(1);

			if (framebufferResized)
			{
				Vector2D<int> framebufferSize = window!.FramebufferSize;

				while (framebufferSize.X == 0 || framebufferSize.Y == 0)
				{
					framebufferSize = window.FramebufferSize;
					window.DoEvents();
				}

				//renderContext.renderPipeline.RecreateSwapchain(context, renderContext.surface, renderContext.commandPool);
				framebufferResized = false;
			}
		}

		[SystemUpdate, SystemLayer(0)]
		public void UpdateCamera(ref VulkanRenderContext context, Position.Ref position, Rotation.Ref rotation, Camera.Ref camera, ref VkSkybox skybox)
		{
			/*
			UpdateCameraUbo2(ref context.ubo, camera, position, rotation);
			context.ubo.cameraPos = new Vector3(position.x, position.y, position.z);
			context.skybox = skybox;

            // SkyboxTexture
            renderContext.texturePipeline.StartRender(this.context);
            renderContext.texturePipeline.StartRenderPass(this.context, RenderPassId.Skybox, PipelineContainerLayer.Skybox);

            ref DefaultDescriptorSet set2 = ref renderContext.texturePipeline.GetDescriptor(this.context, 0);
            set2.shaderInput.ubo.Value = context.ubo;
            UpdateFrameDescriptorSet(this.context, set.descriptorSet, skyboxTextureBuffer);

            renderContext.texturePipeline.Render(this.context, PipelineContainerLayer.Pbr, skyboxBuffer.vertexBuffer, skyboxBuffer.indexBuffer, skyboxBuffer.indicies, 0);
            renderContext.texturePipeline.EndRenderPass(this.context);
            renderContext.texturePipeline.PresentRender(this.context);

            // Skybox render
            renderContext.pipeline.StartRender(this.context);
            renderContext.pipeline.StartRenderPass(this.context, RenderPassId.Skybox, PipelineContainerLayer.Skybox);

            ref DefaultDescriptorSet set = ref renderContext.pipeline.GetDescriptor(this.context, 0);
            set.shaderInput.ubo.Value = context.ubo;
            UpdateSkyboxDescriptorSet(this.context, set.descriptorSet, skybox.skybox);

            renderContext.pipeline.Render(this.context, PipelineContainerLayer.Skybox, skyboxBuffer.vertexBuffer, skyboxBuffer.indexBuffer, skyboxBuffer.indicies, 0);
            renderContext.pipeline.ClearDepthBuffer(this.context); // Clear depth buffer because mesh rendering might go over multiple render passes, so depth buffer is loaded for each pass.
            renderContext.pipeline.EndRenderPass(this.context);

            UpdateCameraUbo(ref context.ubo, camera, position, rotation);
			*/
        }

		int bufferIdx;
		int renderIdx;

		[SystemPreLoop, SystemLayer(1, 2)]
		public void PreRenderPass()
		{
			/*
			renderContext.pipeline.StartRenderPass(context, RenderPassId.Mesh, PipelineContainerLayer.Pbr);

            bufferIdx = 0;
			renderIdx = 0;
			*/
		}

		[SystemUpdate, SystemLayer(1, 2)]
		public void BufferUpdate(ref VulkanRenderContext context, Position.Ref position, Rotation.Ref rotation, Scale.Ref scale, ref VkMeshBuffer mesh, ref VkPbrMaterial material)
		{
			/*
            UpdateEntityUbo(ref context.ubo, position, rotation, scale);

            ref DefaultDescriptorSet set = ref renderContext.pipeline.GetDescriptor(this.context, bufferIdx);
			set.shaderInput.ubo.Value = context.ubo;

            set.shaderInput.material.Value = PbrMaterialInfo.FromMaterial(material);
            for (int i = 0; i < 4; i++)
            {
                set.shaderInput.lights[i].Value = defaultLights[i];
            }

            VulkanRenderHelpers.UpdateMeshDescriptorSet(this.context, set.descriptorSet, skyboxHdrTextureBuffer, material, context.skybox, renderContext.samplers);

            bufferIdx++;
			*/
		}

		[SystemUpdate, SystemLayer(1, 2)]
		public void RenderUpdate(ref VulkanRenderContext context, Position.Ref position, Rotation.Ref rotation, Scale.Ref scale, ref VkMeshBuffer mesh, ref VkPbrMaterial material)
		{
			/*
			renderContext.pipeline.Render(this.context, PipelineContainerLayer.Pbr, mesh.vertexBuffer, mesh.indexBuffer, mesh.indicies, renderIdx);
			renderIdx++;
			*/
		}

		[SystemPostLoop, SystemLayer(1, 2)]
		public void PostRenderPass()
		{
			//renderContext.pipeline.EndRenderPass(context);
		}


		[SystemPostLoop, SystemLayer(0)]
		public void PostRender()
		{
			//renderContext.pipeline.PresentRender(context);
        }
    }

	static class VulkanRenderHelpers
	{
		public static unsafe void UpdateGuiDescriptorSet(VkContext context, DescriptorSet descriptorSet, VkTextureBuffer textureMap, Sampler sampler)
		{
			Span<DescriptorImageInfo> infos = stackalloc DescriptorImageInfo[1];
			Span<WriteDescriptorSet> descriptorWrites = stackalloc WriteDescriptorSet[1];

			CreateDescriptorWrite(ref infos[0], ref descriptorWrites[0], 1, textureMap, sampler, descriptorSet);

			context.vk.UpdateDescriptorSets(context.device, descriptorWrites, 0, null);
		}

		public static unsafe void UpdateMeshDescriptorSet(VkContext context, DescriptorSet descriptorSet, VkTextureBuffer texture, VkPbrMaterial material, VkSkybox skybox, Span<Sampler> samplers)
		{
			Span<DescriptorImageInfo> infos = stackalloc DescriptorImageInfo[9];
			Span<WriteDescriptorSet> descriptorWrites = stackalloc WriteDescriptorSet[9];

            CreateDescriptorWrite(ref infos[0], ref descriptorWrites[0], 1, texture,				samplers[0], descriptorSet);
			CreateDescriptorWrite(ref infos[1], ref descriptorWrites[1], 2, material.albedoMap,		samplers[1], descriptorSet);
			CreateDescriptorWrite(ref infos[2], ref descriptorWrites[2], 3, material.normalMap,		samplers[2], descriptorSet);
			CreateDescriptorWrite(ref infos[3], ref descriptorWrites[3], 4, material.metallicMap,	samplers[3], descriptorSet);
			CreateDescriptorWrite(ref infos[4], ref descriptorWrites[4], 5, material.roughnessMap,	samplers[4], descriptorSet);
			CreateDescriptorWrite(ref infos[4], ref descriptorWrites[5], 6, material.depthMap,		samplers[5], descriptorSet);
			CreateDescriptorWrite(ref infos[5], ref descriptorWrites[6], 7, skybox.skybox,			samplers[6], descriptorSet);
			CreateDescriptorWrite(ref infos[6], ref descriptorWrites[7], 8, skybox.irradiance,		samplers[7], descriptorSet);
			CreateDescriptorWrite(ref infos[7], ref descriptorWrites[8], 9, skybox.specular,		samplers[8], descriptorSet);

			context.vk.UpdateDescriptorSets(context.device, descriptorWrites, 0, null);
		}

		public static unsafe void UpdateSkyboxDescriptorSet(VkContext context, DescriptorSet descriptorSet, VkTextureBuffer skybox, Span<Sampler> samplers)
		{
			Span<DescriptorImageInfo> infos = stackalloc DescriptorImageInfo[1];
			Span<WriteDescriptorSet> descriptorWrites = stackalloc WriteDescriptorSet[1];

			CreateDescriptorWrite(ref infos[0], ref descriptorWrites[0], 6, skybox, samplers[5], descriptorSet);

			context.vk.UpdateDescriptorSets(context.device, descriptorWrites, 0, null);
		}

		static unsafe void CreateDescriptorWrite(ref DescriptorImageInfo imageInfo, ref WriteDescriptorSet imageDescriptorWrite, uint binding, VkTextureBuffer textureBuffer, Sampler sampler, DescriptorSet descriptorSet)
		{
			imageInfo.ImageLayout = ImageLayout.ReadOnlyOptimal;
			imageInfo.ImageView = textureBuffer.textureImageView;
			imageInfo.Sampler = sampler;

			imageDescriptorWrite.SType = StructureType.WriteDescriptorSet;
			imageDescriptorWrite.DstSet = descriptorSet;
			imageDescriptorWrite.DstBinding = binding;
			imageDescriptorWrite.DstArrayElement = 0;
			imageDescriptorWrite.DescriptorType = DescriptorType.CombinedImageSampler;
			imageDescriptorWrite.DescriptorCount = 1;
			imageDescriptorWrite.PImageInfo = (DescriptorImageInfo*)Unsafe.AsPointer(ref imageInfo);
		}
	}
}
