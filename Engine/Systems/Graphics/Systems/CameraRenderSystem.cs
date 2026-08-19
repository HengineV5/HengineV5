using EnCS.Attributes;
using Engine.Components;
using Engine.Graphics;
using Silk.NET.Windowing;

using RenderLib;


namespace Engine
{
	[System]
	[SystemContext<RenderContext>]
	[UsingResource<SkyboxResourceManager>]
	public partial class CameraRenderSystem
	{
		IWindow window;
		GraphicsContext renderContext;

		MeshBuffer skyboxBuffer;

		public CameraRenderSystem(GraphicsContext renderContext, IWindow window)
		{
			this.renderContext = renderContext;
			this.window = window;
		}

		public void Init()
		{
			var storage = renderContext.Storage;

			var skyboxMesh = Mesh.LoadOBJ("Skybox", "Models/Skybox.obj");
			skyboxBuffer = MeshBufferFactory.CreateMeshBuffer(ref storage, skyboxMesh);
		}

		[SystemUpdate]
		public void UpdateCamera(ref RenderContext context, ref Position position, ref Rotation rotation, ref Camera camera, ref SkyboxBuffer skybox)
		{
			var storage = renderContext.Storage;

			UpdateSkyboxCameraUbo(ref context.skyboxUbo, ref camera, ref rotation, window);
			context.skyboxUbo.cameraPos = new Vector3f(position.x, position.y, position.z);
			context.pbrUbo.cameraPos = new Vector3f(position.x, position.y, position.z);
			context.gizmoUbo.cameraPos = new Vector3f(position.x, position.y, position.z);
			context.skybox = skybox;

			// Skybox render
			renderContext.pipeline.StartRender(ref storage);
			renderContext.pipeline.StartRenderPass(ref storage, RenderPassId.Skybox, PipelineContainerLayer.Skybox);

			ref PbrShaderInput<RenderStorage> shaderInput = ref renderContext.pipeline.GetUbo<PbrShaderInput<RenderStorage>>(ref storage, 0);
			shaderInput.ubo.Value = context.skyboxUbo;
			DescriptorSetWriter.UpdateSkyboxDescriptorSet(ref storage, renderContext.pipeline.GetDescriptorSet(ref storage, PipelineContainerLayer.Skybox, 0), skybox.skybox, renderContext.samplers);

			renderContext.pipeline.Render(ref storage, PipelineContainerLayer.Skybox, skyboxBuffer.vertexBuffer, skyboxBuffer.indexBuffer, skyboxBuffer.indicies, 0);
			renderContext.pipeline.ClearDepthBuffer(ref storage); // Clear depth buffer because mesh rendering might go over multiple render passes, so depth buffer is loaded for each pass.
			renderContext.pipeline.EndRenderPass(ref storage);

			UpdateCameraUbo(ref context.pbrUbo, ref camera, ref position, ref rotation, window);
			UpdateCameraGuiUbo(ref context.guiUbo, ref camera, window);
			UpdateCameraGizmoUbo(ref context.gizmoUbo, ref camera, ref position, ref rotation, window);
		}

		static void UpdateCameraUbo(ref MeshUniformBufferObject ubo, ref Camera camera, ref Position position, ref Rotation rotation, IWindow window)
		{
			ubo.view = Matrix4x4f.CreateTranslation(-new Vector3f(position.x, position.y, position.z)) * Matrix4x4f.FromQuaternion(new Quaternionf(rotation.x, rotation.y, rotation.z, rotation.w));
			//ubo.proj = Matrix4x4f.CreatePerspectiveFieldOfView(camera.fov, camera.width / camera.height, camera.zNear, camera.zFar);
			ubo.proj = Matrix4x4f.CreatePersperctive(camera.fov, (float)window.Size.X / (float)window.Size.Y, camera.zNear, camera.zFar);

			ubo.proj.m22 *= -1; // Think this was some opengl comaptability stuff.
		}

		static void UpdateSkyboxCameraUbo(ref MeshUniformBufferObject ubo, ref Camera camera, ref Rotation rotation, IWindow window)
		{
			ubo.view = Matrix4x4f.FromQuaternion(new Quaternionf(rotation.x, rotation.y, rotation.z, rotation.w));
			//ubo.proj = Matrix4x4f.CreatePerspectiveFieldOfView(camera.fov, camera.width / camera.height, camera.zNear, camera.zFar);
			ubo.proj = Matrix4x4f.CreatePersperctive(camera.fov, (float)window.Size.X / (float)window.Size.Y, camera.zNear, camera.zFar);

			ubo.proj.m22 *= -1; // Think this was some opengl comaptability stuff.
		}

		static void UpdateCameraGizmoUbo(ref MeshUniformBufferObject ubo, ref Camera camera, ref Position position, ref Rotation rotation, IWindow window)
		{
			ubo.view = Matrix4x4f.CreateTranslation(-new Vector3f(position.x, position.y, position.z)) * Matrix4x4f.FromQuaternion(new Quaternionf(rotation.x, rotation.y, rotation.z, rotation.w));
			ubo.proj = Matrix4x4f.CreatePersperctive(camera.fov, (float)window.Size.X / (float)window.Size.Y, camera.zNear, camera.zFar);
			//ubo.proj = Matrix4x4f.CreatePerspectiveFieldOfView(MathF.PI / 2, (float)window.Size.X / (float)window.Size.Y, camera.zNear, camera.zFar);
			//ubo.proj = Matrix4x4f.CreateOrthographic(10, 10, camera.zNear, camera.zFar);

			ubo.proj.m22 *= -1; // Think this was some opengl comaptability stuff.
		}

		static void UpdateCameraGuiUbo(ref GuiUniformBufferObject ubo, ref Camera camera, IWindow window)
		{
			//ubo.proj = Matrix4x4f.CreatePerspectiveFieldOfView(camera.fov, camera.width / camera.height, camera.zNear, camera.zFar);
			//ubo.proj = Matrix4x4f.CreatePerspectiveFieldOfView(camera.fov, (float)window.Size.X / (float)window.Size.Y, camera.zNear, camera.zFar);
			ubo.proj = Matrix4x4f.CreatePersperctive(1.57f, 1, 0.1f, 100);
			ubo.screenSize = new Vector2f(window.Size.X, window.Size.Y);

			//ubo.proj.M22 *= -1; // Think this was some opengl comaptability stuff.
		}
	}
}
