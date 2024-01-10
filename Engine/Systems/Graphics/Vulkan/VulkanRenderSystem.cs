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
using System.Runtime.InteropServices;
using System.Text;

namespace Engine
{
	public class UniformBufferBuilder
	{
		int idx = 0;
		Memory<DescriptorBufferInfo> bufferInfos = new DescriptorBufferInfo[128];
		Memory<WriteDescriptorSet> descriptorWrites = new WriteDescriptorSet[128];

		ulong offset = 0;
		DescriptorSet descriptor;
		Silk.NET.Vulkan.Buffer buffer;


		public UniformBufferBuilder(DescriptorSet descriptor, Silk.NET.Vulkan.Buffer buffer)
        {
            this.descriptor = descriptor;
			this.buffer = buffer;
        }

		public unsafe UniformBufferBuilder Variable<T>(uint binding, uint arrayElement = 0) where T : unmanaged
		{
			DescriptorBufferInfo bufferInfo = new();
			bufferInfo.Buffer = buffer;
			bufferInfo.Offset = offset;
			bufferInfo.Range = (ulong)sizeof(T);

			bufferInfos.Span[idx] = bufferInfo;

			WriteDescriptorSet descriptorWrite = new();
			descriptorWrite.SType = StructureType.WriteDescriptorSet;
			descriptorWrite.DstSet = descriptor;
			descriptorWrite.DstBinding = binding;
			descriptorWrite.DstArrayElement = arrayElement;
			descriptorWrite.DescriptorType = DescriptorType.UniformBuffer;
			descriptorWrite.DescriptorCount = 1;
			fixed(DescriptorBufferInfo* infoPtr = &bufferInfos.Span[idx])
			{
				descriptorWrite.PBufferInfo = infoPtr;
			}

			offset += 64 * ((ulong)sizeof(T) / 64 + 1);
			descriptorWrites.Span[idx] = descriptorWrite;
			idx++;

			return this;
		}

		public UniformBufferBuilder Array<T>(uint binding, uint size) where T : unmanaged
		{
			for (uint i = 0; i < size; i++)
			{
				Variable<T>(binding, i);
			}

			return this;
		}

		public ulong GetSize()
		{
			return offset;
		}

		public ulong GetOffset(uint idx)
		{
			return bufferInfos.Span[(int)idx].Offset;
		}

		public unsafe MappedMemory<T> GetElement<T>(void* dataPtr, uint idx) where T : unmanaged
		{
			return new((T*)Unsafe.Add<byte>(dataPtr, (int)GetOffset(idx)));
		}

		public unsafe void UpdateDescriptorSet(VkContext context)
		{
			context.vk.UpdateDescriptorSets(context.device, descriptorWrites.Span.Slice(0, idx), 0, null);
		}
    }

	[StructLayout(LayoutKind.Explicit)]
	public struct UniformBufferObject
	{
		[FieldOffset(0)]
		public Matrix4x4 translation;

		[FieldOffset(64)]
		public Matrix4x4 rotation;

		[FieldOffset(128)]
		public Matrix4x4 scale;

		[FieldOffset(192)]
		public Matrix4x4 view;

		[FieldOffset(256)]
		public Matrix4x4 proj;

		[FieldOffset(320)]
		public Vector3 cameraPos;
	}

	public struct VulkanShaderInput
	{
		public MappedMemory<UniformBufferObject> ubo;
		//public MappedMemory<Material> material;
		public MappedMemory<PbrMaterial> material;
		public FixedArray4<MappedMemory<Light>> lights;
	}

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
		public UniformBufferObject ubo;

        public VulkanRenderContext()
        {
			this.ubo = new UniformBufferObject();
        }
    }

	[System<VulkanRenderContext>]
	[UsingResource<VulkanMeshResourceManager>]
	[UsingResource<VulkanTextureResourceManager>]
	public partial class VulkanRenderSystem
	{
		private static readonly Material defaultMaterial = new Material
		{
			Ambient = new Vector3(0.0215f, 0.1745f, 0.0215f),
			Diffuse = new Vector3(0.07568f, 0.61424f, 0.07568f),
			Specular = new Vector3(0.633f, 0.727811f, 0.633f),
			Shininess = 2f
		};

		private static PbrMaterial defaultPbrMaterial = new PbrMaterial
		{
			albedo = new Vector3(1, 0, 0),
			metallic = 0.95f,
			roughness = 0f
		};

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
		VkTextureBuffer skyboxTextureBuffer;
		VkTextureBuffer albedoTextureBuffer;
		VkTextureBuffer normalTextureBuffer;
		VkTextureBuffer metallicTextureBuffer;
		VkTextureBuffer roughnessTextureBuffer;

		public VulkanRenderSystem(VkContext context, VkRenderContext renderContext, IWindow window)
		{
			this.context = context;
			this.renderContext = renderContext;
			this.window = window;

			defaultLights[0] = defaultLight;
			defaultLights[1] = defaultLight;
			defaultLights[2] = defaultLight;
			defaultLights[3] = defaultLight;

			defaultLights[0].Position = new Vector3(0, 3, -5);
			defaultLights[1].Position = new Vector3(10, 0, -2);
			defaultLights[2].Position = new Vector3(-10, 0, -2);
			defaultLights[3].Position = new Vector3(0, 10, -2);
		}

		public void Init()
		{
			var skyboxMesh = Mesh.LoadOBJ("Skybox", "Models/Skybox.obj");
			skyboxBuffer = VulkanMeshResourceManager.CreateMeshBuffer(context, skyboxMesh);

			var textureCubemap = ETexture.LoadImage("Skybox", "Images/Skybox/skybox_new.png");
			skyboxTextureBuffer = VulkanTextureResourceManager.CreateCubeTextureBuffer(context, textureCubemap);

            var textureAlbedo = ETexture.LoadImage("PbrGoldAlbedo", "Images/Pbr/Gold/gold-scuffed_basecolor-boosted.png");
			//var textureAlbedo = ETexture.LoadImage("PbrGoldAlbedo", "Images/Pbr/Iron/rustediron2_basecolor.png");
			albedoTextureBuffer = VulkanTextureResourceManager.CreateTextureBuffer(context, textureAlbedo);

			var textureNormal = ETexture.LoadImage("PbrGoldAlbedo", "Images/Pbr/Gold/gold-scuffed_normal.png");
			//var textureNormal = ETexture.LoadImage("PbrGoldAlbedo", "Images/Pbr/Iron/rustediron2_normal.png");
			normalTextureBuffer = VulkanTextureResourceManager.CreateTextureBuffer(context, textureNormal);

			var textureMetallic = ETexture.LoadImage("PbrGoldAlbedo", "Images/Pbr/Gold/gold-scuffed_metallic.png");
			//var textureMetallic = ETexture.LoadImage("PbrGoldAlbedo", "Images/Pbr/Iron/rustediron2_metallic.png");
			metallicTextureBuffer = VulkanTextureResourceManager.CreateTextureBuffer(context, textureMetallic);

			var textureRoughness = ETexture.LoadImage("PbrGoldAlbedo", "Images/Pbr/Gold/gold-scuffed_roughness.png");
			//var textureRoughness = ETexture.LoadImage("PbrGoldAlbedo", "Images/Pbr/Iron/rustediron2_roughness.png");
			roughnessTextureBuffer = VulkanTextureResourceManager.CreateTextureBuffer(context, textureRoughness);

			window.FramebufferResize += Window_Resize;
		}

		private void Window_Resize(Vector2D<int> obj)
		{
			framebufferResized = true;
		}

		public unsafe void Dispose()
		{
			renderContext.renderPipeline.Dispose(context);
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

				renderContext.renderPipeline.RecreateSwapchain(context, renderContext.surface, renderContext.commandPool);
				framebufferResized = false;
			}
		}

		[SystemUpdate, SystemLayer(0)]
		public void UpdateCamera(ref VulkanRenderContext context, Position.Ref position, Rotation.Ref rotation, Camera.Ref camera)
		{
			UpdateCameraUbo2(ref context.ubo, camera, position, rotation);
			context.ubo.cameraPos = new Vector3(position.x, position.y, position.z);

            var result = renderContext.renderPipeline.StartRender(this.context, ref context.ubo, skyboxTextureBuffer.textureImageView, skyboxBuffer.vertexBuffer, skyboxBuffer.indexBuffer, skyboxBuffer.indicies);
            if (result == Result.ErrorOutOfDateKhr)
            {
                renderContext.renderPipeline.RecreateSwapchain(this.context, renderContext.surface, renderContext.commandPool);
                renderContext.renderPipeline.StartRender(this.context, ref context.ubo, skyboxTextureBuffer.textureImageView, skyboxBuffer.vertexBuffer, skyboxBuffer.indexBuffer, skyboxBuffer.indicies);
            }

            UpdateCameraUbo(ref context.ubo, camera, position, rotation);
        }

		int bufferIdx;
		int renderIdx;

		[SystemPreLoop, SystemLayer(0)]
		public void PreRender()
		{
			/*
			var result = renderContext.renderPipeline.StartRender(context);
			if (result == Result.ErrorOutOfDateKhr)
			{
				renderContext.renderPipeline.RecreateSwapchain(context, renderContext.surface, renderContext.commandPool);
				renderContext.renderPipeline.StartRender(context);
			}
			*/
		}

		[SystemPreLoop, SystemLayer(1, 2)]
		public void PreRenderPass()
		{
			renderContext.renderPipeline.BeginRenderPass(context);

			bufferIdx = 0;
			renderIdx = 0;
		}

		[SystemUpdate, SystemLayer(1, 2)]
		public void BufferUpdate(ref VulkanRenderContext context, Position.Ref position, Rotation.Ref rotation, Scale.Ref scale, ref VkMeshBuffer mesh, ref VkTextureBuffer textureBuffer)
		{
			renderContext.renderPipeline.UpdateFrameDescriptorSet(this.context, skyboxTextureBuffer.textureImageView, bufferIdx, albedoTextureBuffer, normalTextureBuffer, metallicTextureBuffer, roughnessTextureBuffer);
			bufferIdx++;
		}

		[SystemUpdate, SystemLayer(1, 2)]
		public void RenderUpdate(ref VulkanRenderContext context, Position.Ref position, Rotation.Ref rotation, Scale.Ref scale, ref VkMeshBuffer mesh, ref VkTextureBuffer textureBuffer)
		{
			defaultPbrMaterial.roughness = (1 - ((float)renderIdx / 7));

            UpdateEntityUbo(ref context.ubo, position, rotation, scale);
			renderContext.renderPipeline.Render(this.context, ref context.ubo, defaultPbrMaterial, defaultLights, mesh.vertexBuffer, mesh.indexBuffer, mesh.indicies, renderIdx);
			renderIdx++;
		}

		[SystemPostLoop, SystemLayer(1, 2)]
		public void PostRenderPass()
		{
			renderContext.renderPipeline.EndRenderPass(context);
		}


		[SystemPostLoop, SystemLayer(0)]
		public void PostRender()
		{
			renderContext.renderPipeline.PresentRender(context);
		}

		public void PostRun()
		{
		}

		static void UpdateCameraUbo(ref UniformBufferObject ubo, Camera.Ref camera, Position.Ref position, Rotation.Ref rotation)
		{
			ubo.view = Matrix4x4.CreateTranslation(-new Vector3(position.x, position.y, position.z)) * Matrix4x4.CreateFromQuaternion(new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w));
			ubo.proj = Matrix4x4.CreatePerspectiveFieldOfView(camera.fov, camera.width / camera.height, camera.zNear, camera.zFar);

			ubo.proj.M22 *= -1; // Think this was some opengl comaptability stuff.
		}

        static void UpdateCameraUbo2(ref UniformBufferObject ubo, Camera.Ref camera, Position.Ref position, Rotation.Ref rotation)
        {
            ubo.view = Matrix4x4.CreateFromQuaternion(new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w));
            ubo.proj = Matrix4x4.CreatePerspectiveFieldOfView(camera.fov, camera.width / camera.height, camera.zNear, camera.zFar);

            ubo.proj.M22 *= -1; // Think this was some opengl comaptability stuff.
        }

        static void UpdateEntityUbo(ref UniformBufferObject ubo, Position.Ref position, Rotation.Ref rotation, Scale.Ref scale)
		{
			ubo.translation = Matrix4x4.CreateTranslation(new Vector3(position.x, position.y, position.z));
			ubo.rotation = Matrix4x4.CreateFromQuaternion(new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w));
			ubo.scale = Matrix4x4.CreateScale(new Vector3(scale.x, scale.y, scale.z));
		}
	} 
}
