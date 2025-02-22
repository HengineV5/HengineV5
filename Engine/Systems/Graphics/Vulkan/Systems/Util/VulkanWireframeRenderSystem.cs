using EnCS;
using EnCS.Attributes;
using Engine.Components;
using Engine.Graphics;
using Silk.NET.Windowing;
using System.Numerics;

namespace Engine
{
	[System]
	[SystemContext<VulkanRenderContext>]
	[UsingResource<VulkanMeshResourceManager>]
	[UsingResource<VulkanMaterialResourceManager>]
	public partial class VulkanWireframeRenderSystem
	{
		IWindow window;
		VkContext context;
		VkRenderContext renderContext;
		IInputHandler inputHandler;

		bool wireframeEnabled = false;
		bool keyPressed = false;

		public VulkanWireframeRenderSystem(VkContext context, VkRenderContext renderContext, IWindow window, IInputHandler inputHandler)
		{
			this.context = context;
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
			if (inputHandler.IsKeyDown(Silk.NET.Input.Key.F1) && !keyPressed)
				wireframeEnabled = !wireframeEnabled;

			keyPressed = inputHandler.IsKeyDown(Silk.NET.Input.Key.F1);

			if (wireframeEnabled)
				renderContext.pipeline.StartRenderPass(context, RenderPassId.Mesh, PipelineContainerLayer.Wireframe);

			bufferIdx = 0;
			updateIdx = 0;
		}

		[SystemUpdate, SystemLayer(0, 2)]
		public void BufferUpdate(ref VulkanRenderContext context, Position.Ref position, Rotation.Ref rotation, Scale.Ref scale, ref VkMeshBuffer mesh, ref VkPbrMaterial material)
		{
			if (wireframeEnabled)
			{
				UpdateEntityUbo(ref context.pbrUbo, position, rotation, scale);

				ref PbrShaderInput shaderInput = ref renderContext.pipeline.GetUbo<PbrShaderInput>(bufferIdx);
				shaderInput.ubo.Value = context.pbrUbo;
			}


			bufferIdx++;
		}

		[SystemUpdate, SystemLayer(0, 2)]
		public void RenderUpdate(ref VulkanRenderContext context, Position.Ref position, Rotation.Ref rotation, Scale.Ref scale, ref VkMeshBuffer mesh, ref VkPbrMaterial material)
		{
			if (wireframeEnabled)
				renderContext.pipeline.Render(this.context, PipelineContainerLayer.Wireframe, mesh.vertexBuffer, mesh.indexBuffer, mesh.indicies, updateIdx);

			updateIdx++;
		}

		[SystemPostLoop, SystemLayer(0, 2)]
		public void PostRenderPass()
		{
			if (wireframeEnabled)
				renderContext.pipeline.EndRenderPass(context);
		}

		static void UpdateEntityUbo(ref MeshUniformBufferObject ubo, Position.Ref position, Rotation.Ref rotation, Scale.Ref scale)
		{
			ubo.translation = Matrix4x4f.CreateTranslation(new Vector3f(position.x, position.y, position.z));
			ubo.rotation = Matrix4x4f.FromQuaternion(new Quaternionf(rotation.x, rotation.y, rotation.z, rotation.w));
			ubo.scale = Matrix4x4f.CreateScale(new Vector3f(scale.x, scale.y, scale.z));
		}
	}
}
