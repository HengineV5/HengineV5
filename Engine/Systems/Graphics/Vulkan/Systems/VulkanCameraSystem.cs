using EnCS.Attributes;
using Engine.Components;
using Engine.Graphics;
using Silk.NET.Windowing;
using System.Numerics;

namespace Engine
{
	[System<VulkanRenderContext>]
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
		public void UpdateCamera(ref VulkanRenderContext context, Position.Ref position, Rotation.Ref rotation, Camera.Ref camera, ref VkSkybox skybox)
		{
			UpdateCameraUbo2(ref context.ubo, camera, position, rotation);
			context.ubo.cameraPos = new Vector3(position.x, position.y, position.z);
			context.skybox = skybox;

			// SkyboxTexture
			/*
            renderContext.texturePipeline.StartRender(this.context);
            renderContext.texturePipeline.StartRenderPass(this.context, RenderPassId.Skybox, PipelineContainerLayer.Skybox);

            ref DefaultDescriptorSet set2 = ref renderContext.texturePipeline.GetDescriptor(this.context, 0);
            set2.shaderInput.ubo.Value = context.ubo;
            UpdateFrameDescriptorSet(this.context, set.descriptorSet, skyboxTextureBuffer);

            renderContext.texturePipeline.Render(this.context, PipelineContainerLayer.Pbr, skyboxBuffer.vertexBuffer, skyboxBuffer.indexBuffer, skyboxBuffer.indicies, 0);
            renderContext.texturePipeline.EndRenderPass(this.context);
            renderContext.texturePipeline.PresentRender(this.context);
			*/

			// Skybox render
			renderContext.pipeline.StartRender(this.context);
			renderContext.pipeline.StartRenderPass(this.context, RenderPassId.Skybox, PipelineContainerLayer.Skybox);

			ref DefaultDescriptorSet set = ref renderContext.pipeline.GetDescriptor(this.context, 0);
			set.shaderInput.ubo.Value = context.ubo;
			VulkanRenderHelpers.UpdateSkyboxDescriptorSet(this.context, set.descriptorSet, skybox.skybox, renderContext.samplers);

			renderContext.pipeline.Render(this.context, PipelineContainerLayer.Skybox, skyboxBuffer.vertexBuffer, skyboxBuffer.indexBuffer, skyboxBuffer.indicies, 0);
			renderContext.pipeline.ClearDepthBuffer(this.context); // Clear depth buffer because mesh rendering might go over multiple render passes, so depth buffer is loaded for each pass.
			renderContext.pipeline.EndRenderPass(this.context);

			UpdateCameraUbo(ref context.ubo, camera, position, rotation);
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
	}
}
