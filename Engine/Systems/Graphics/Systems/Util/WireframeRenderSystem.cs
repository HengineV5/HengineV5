using EnCS.Attributes;
using Engine.Components;
using Engine.Graphics;
using Silk.NET.Windowing;

using RenderLib;


namespace Engine
{
	[System]
	[SystemContext<RenderContext>]
	[UsingResource<MeshResourceManager>]
	[UsingResource<MaterialResourceManager>]
	public partial class WireframeRenderSystem
	{
		IWindow window;
		GraphicsContext renderContext;
		IInputHandler inputHandler;

		bool wireframeEnabled = false;
		bool keyPressed = false;

		public WireframeRenderSystem(GraphicsContext renderContext, IWindow window, IInputHandler inputHandler)
		{
			this.renderContext = renderContext;
			this.window = window;
			this.inputHandler = inputHandler;
		}

		public void Init()
		{
		}

		// TODO: Refactor out
		int bufferIdx;
		int updateIdx;

		[SystemPreLoop, SystemLayer(0, 2)]
		public void PreRenderPass()
		{
			var storage = renderContext.Storage;

			if (inputHandler.IsKeyDown(Silk.NET.Input.Key.F1) && !keyPressed)
				wireframeEnabled = !wireframeEnabled;

			keyPressed = inputHandler.IsKeyDown(Silk.NET.Input.Key.F1);

			if (wireframeEnabled)
				renderContext.pipeline.StartRenderPass(ref storage, RenderPassId.Mesh, PipelineContainerLayer.Wireframe);

			bufferIdx = 0;
			updateIdx = 0;
		}

		[SystemUpdate, SystemLayer(0, 2)]
		public void BufferUpdate(ref RenderContext context, ref Position position, ref Rotation rotation, ref Scale scale, ref MeshBuffer mesh, ref PbrMaterialBuffer material)
		{
			var storage = renderContext.Storage;

			if (wireframeEnabled)
			{
				UpdateEntityUbo(ref context.pbrUbo, ref position, ref rotation, ref scale);

				ref PbrShaderInput<RenderStorage> shaderInput = ref renderContext.pipeline.GetUbo<PbrShaderInput<RenderStorage>>(ref storage, bufferIdx);
				shaderInput.ubo.Value = context.pbrUbo;
			}


			bufferIdx++;
		}

		[SystemUpdate, SystemLayer(0, 2)]
		public void RenderUpdate(ref RenderContext context, ref Position position, ref Rotation rotation, ref Scale scale, ref MeshBuffer mesh, ref PbrMaterialBuffer material)
		{
			var storage = renderContext.Storage;

			if (wireframeEnabled)
				renderContext.pipeline.Render(ref storage, PipelineContainerLayer.Wireframe, mesh.vertexBuffer, mesh.indexBuffer, mesh.indicies, updateIdx);

			updateIdx++;
		}

		[SystemPostLoop, SystemLayer(0, 2)]
		public void PostRenderPass()
		{
			var storage = renderContext.Storage;

			if (wireframeEnabled)
				renderContext.pipeline.EndRenderPass(ref storage);
		}

		static void UpdateEntityUbo(ref MeshUniformBufferObject ubo, ref Position position, ref Rotation rotation, ref Scale scale)
		{
			ubo.translation = Matrix4x4f.CreateTranslation(new Vector3f(position.x, position.y, position.z));
			ubo.rotation = Matrix4x4f.FromQuaternion(new Quaternionf(rotation.x, rotation.y, rotation.z, rotation.w));
			ubo.scale = Matrix4x4f.CreateScale(new Vector3f(scale.x, scale.y, scale.z));
		}
	}
}
