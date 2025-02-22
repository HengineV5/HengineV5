using EnCS.Attributes;
using Engine.Components;
using Engine.Graphics;
using Silk.NET.Windowing;
using System.Numerics;

namespace Engine
{
	[System]
	[SystemContext<VulkanRenderContext>]
	[UsingResource<VulkanSkyboxResourceManager>]
	public partial class VulkanCameraSystem
	{
		IWindow window;
		VkContext context;
		VkRenderContext renderContext;

		VkMeshBuffer skyboxBuffer;

		public VulkanCameraSystem(VkContext context, VkRenderContext renderContext, IWindow window)
		{
			this.context = context;
			this.renderContext = renderContext;
			this.window = window;
		}

		public void Init()
		{
			var skyboxMesh = Mesh.LoadOBJ("Skybox", "Models/Skybox.obj");
			skyboxBuffer = VulkanMeshResourceManager.CreateMeshBuffer(context, skyboxMesh);
		}

		[SystemUpdate]
		public void UpdateCamera(ref VulkanRenderContext context, in Position.Ref position, in Rotation.Ref rotation, in Camera.Ref camera, ref VkSkybox skybox)
		{
			UpdateSkyboxCameraUbo(ref context.skyboxUbo, camera, rotation, window);
			context.skyboxUbo.cameraPos = new Vector3f(position.x, position.y, position.z);
			context.pbrUbo.cameraPos = new Vector3f(position.x, position.y, position.z);
			context.gizmoUbo.cameraPos = new Vector3f(position.x, position.y, position.z);
			context.skybox = skybox;

			// Skybox render
			renderContext.pipeline.StartRender(this.context);
			renderContext.pipeline.StartRenderPass(this.context, RenderPassId.Skybox, PipelineContainerLayer.Skybox);

			ref PbrShaderInput shaderInput = ref renderContext.pipeline.GetUbo<PbrShaderInput>(0);
			shaderInput.ubo.Value = context.skyboxUbo;
			VulkanRenderHelpers.UpdateSkyboxDescriptorSet(this.context, renderContext.pipeline.GetDescriptorSet(PipelineContainerLayer.Skybox, 0), skybox.skybox, renderContext.samplers);

            renderContext.pipeline.Render(this.context, PipelineContainerLayer.Skybox, skyboxBuffer.vertexBuffer, skyboxBuffer.indexBuffer, skyboxBuffer.indicies, 0);
			renderContext.pipeline.ClearDepthBuffer(this.context); // Clear depth buffer because mesh rendering might go over multiple render passes, so depth buffer is loaded for each pass.
			renderContext.pipeline.EndRenderPass(this.context);

			UpdateCameraUbo(ref context.pbrUbo, camera, position, rotation, window);
			UpdateCameraGuiUbo(ref context.guiUbo, camera, window);
			UpdateCameraGizmoUbo(ref context.gizmoUbo, camera, position, rotation, window);
		}

		static void UpdateCameraUbo(ref MeshUniformBufferObject ubo, in Camera.Ref camera, in Position.Ref position, in Rotation.Ref rotation, IWindow window)
		{
			ubo.view = Matrix4x4f.CreateTranslation(-new Vector3f(position.x, position.y, position.z)) * Matrix4x4f.FromQuaternion(new Quaternionf(rotation.x, rotation.y, rotation.z, rotation.w));
			//ubo.proj = Matrix4x4f.CreatePerspectiveFieldOfView(camera.fov, camera.width / camera.height, camera.zNear, camera.zFar);
			ubo.proj = Matrix4x4f.CreatePersperctive(camera.fov, (float)window.Size.X / (float)window.Size.Y, camera.zNear, camera.zFar);

			ubo.proj.m22 *= -1; // Think this was some opengl comaptability stuff.
		}

		static void UpdateSkyboxCameraUbo(ref MeshUniformBufferObject ubo, in Camera.Ref camera, in Rotation.Ref rotation, IWindow window)
		{
			ubo.view = Matrix4x4f.FromQuaternion(new Quaternionf(rotation.x, rotation.y, rotation.z, rotation.w));
			//ubo.proj = Matrix4x4f.CreatePerspectiveFieldOfView(camera.fov, camera.width / camera.height, camera.zNear, camera.zFar);
			ubo.proj = Matrix4x4f.CreatePersperctive(camera.fov, (float)window.Size.X / (float)window.Size.Y, camera.zNear, camera.zFar);

			ubo.proj.m22 *= -1; // Think this was some opengl comaptability stuff.
		}

		static void UpdateCameraGizmoUbo(ref MeshUniformBufferObject ubo, in Camera.Ref camera, in Position.Ref position, in Rotation.Ref rotation, IWindow window)
		{
			ubo.view = Matrix4x4f.CreateTranslation(-new Vector3f(position.x, position.y, position.z)) * Matrix4x4f.FromQuaternion(new Quaternionf(rotation.x, rotation.y, rotation.z, rotation.w));
			ubo.proj = Matrix4x4f.CreatePersperctive(camera.fov, (float)window.Size.X / (float)window.Size.Y, camera.zNear, camera.zFar);
            //ubo.proj = Matrix4x4f.CreatePerspectiveFieldOfView(MathF.PI / 2, (float)window.Size.X / (float)window.Size.Y, camera.zNear, camera.zFar);
            //ubo.proj = Matrix4x4f.CreateOrthographic(10, 10, camera.zNear, camera.zFar);

            ubo.proj.m22 *= -1; // Think this was some opengl comaptability stuff.
		}

		static void UpdateCameraGuiUbo(ref GuiUniformBufferObject ubo, in Camera.Ref camera, IWindow window)
		{
			//ubo.proj = Matrix4x4f.CreatePerspectiveFieldOfView(camera.fov, camera.width / camera.height, camera.zNear, camera.zFar);
			//ubo.proj = Matrix4x4f.CreatePerspectiveFieldOfView(camera.fov, (float)window.Size.X / (float)window.Size.Y, camera.zNear, camera.zFar);
			ubo.proj = Matrix4x4f.CreatePersperctive(1.57f, 1, 0.1f, 100);
			ubo.screenSize = new Vector2f(window.Size.X, window.Size.Y);

			//ubo.proj.M22 *= -1; // Think this was some opengl comaptability stuff.
		}
	}
}
