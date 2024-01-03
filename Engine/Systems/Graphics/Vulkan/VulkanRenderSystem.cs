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
using System.Runtime.InteropServices;
using System.Text;

namespace Engine
{
	public struct UniformBufferObject
	{
		public Matrix4x4 translation;
		public Matrix4x4 rotation;
		public Matrix4x4 scale;
		public Matrix4x4 view;
		public Matrix4x4 proj;
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
		IWindow window;
		VkContext context;
		VkRenderContext renderContext;

		bool framebufferResized = false;

		public VulkanRenderSystem(VkContext context, VkRenderContext renderContext, IWindow window)
		{
			this.context = context;
			this.renderContext = renderContext;
			this.window = window;
		}

		public void Init()
		{
			window.FramebufferResize += Window_Resize;

			//VulkanHelper.PrintValidationLayers(context);
			//VulkanHelper.PrintQueueFamilies(context);
			//VulkanHelper.PrintExtensions(context);
			//VulkanHelper.PrintSurfaceCapabilities(context, surface);
			//VulkanHelper.PrintMemoryTypes(context);
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
				renderContext.renderPipeline.RecreateSwapchain(context, renderContext.surface, renderContext.commandPool);
				framebufferResized = false;
			}

			var result = renderContext.renderPipeline.StartRender(context);
			if (result == Result.ErrorOutOfDateKhr)
			{
				renderContext.renderPipeline.RecreateSwapchain(context, renderContext.surface, renderContext.commandPool);
				renderContext.renderPipeline.StartRender(context);
			}
		}

		[SystemUpdate, SystemLayer(0)]
		public void UpdateCamera(ref VulkanRenderContext context, Position.Ref position, Rotation.Ref rotation, Camera.Ref camera)
		{
			UpdateCameraUbo(ref context.ubo, camera, position, rotation);
		}

		public void BufferUpdate(ref VulkanRenderContext context)
		{

		}

		[SystemUpdate, SystemLayer(1)]
		public void RenderUpdate(ref VulkanRenderContext context, Position.Ref position, Rotation.Ref rotation, Scale.Ref scale, ref VkMeshBuffer mesh, ref VkTextureBuffer textureBuffer)
		{
			UpdateEntityUbo(ref context.ubo, position, rotation, scale);
			renderContext.renderPipeline.Render(this.context, ref context.ubo, mesh.vertexBuffer, mesh.indexBuffer, mesh.indicies, textureBuffer.textureImageView);
		}

		public void PostRun()
		{
			renderContext.renderPipeline.EndRender(context);
		}

		static void UpdateCameraUbo(ref UniformBufferObject ubo, Camera.Ref camera, Position.Ref position, Rotation.Ref rotation)
		{
			ubo.view = Matrix4x4.CreateTranslation(-new Vector3(position.x, position.y, position.z)) * Matrix4x4.CreateFromQuaternion(new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w));
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
