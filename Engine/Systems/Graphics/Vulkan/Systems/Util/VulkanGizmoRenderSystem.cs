using EnCS.Attributes;
using Engine.Components;
using Engine.Graphics;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
	[System]
	[SystemContext<VulkanRenderContext>]
	public partial class VulkanGizmoRenderSystem
	{
		VkContext context;
		VkRenderContext renderContext;

		Sampler sampler;
		VkMeshBuffer arrow;

		public VulkanGizmoRenderSystem(VkContext context, VkRenderContext renderContext)
		{
			this.context = context;
			this.renderContext = renderContext;
		}

		public void Init()
		{
			sampler = VulkanHelper.CreateSampler(context, 5);

			arrow = VulkanMeshResourceManager.CreateGizmoBuffer(context, GizmoMeshes.Arrow);
		}

		// TODO: Refactor out
		int bufferIdx;
		int updateIdx;

		[SystemPreLoop, SystemLayer(0, 2)]
		public void PreRenderPass()
		{
			renderContext.pipeline.StartRenderPass(context, RenderPassId.Mesh, PipelineContainerLayer.Gizmo);

			bufferIdx = 0;
			updateIdx = 0;
		}

		[SystemUpdate, SystemLayer(0, 2)]
		public void BufferUpdate(ref VulkanRenderContext context, Position.Ref position, Rotation.Ref rotation, Scale.Ref scale, GizmoComp.Ref gizmoComp)
		{
			UpdateEntityUbo(ref context.pbrUbo, position, rotation, scale);

			ref GizmoShaderInput shaderInput = ref renderContext.pipeline.GetUbo<GizmoShaderInput>(bufferIdx);
			shaderInput.ubo.Value = context.pbrUbo;

			bufferIdx++;
		}

		[SystemUpdate, SystemLayer(0, 2)]
		public void RenderUpdate(ref VulkanRenderContext context, Position.Ref position, Rotation.Ref rotation, Scale.Ref scale, GizmoComp.Ref gizmoComp)
		{
			renderContext.pipeline.Render(this.context, PipelineContainerLayer.Gizmo, arrow.vertexBuffer, arrow.indexBuffer, arrow.indicies, updateIdx);

			updateIdx++;
		}

		[SystemPostLoop, SystemLayer(0, 2)]
		public void PostRenderPass()
		{
			renderContext.pipeline.EndRenderPass(context);
		}

		static void UpdateEntityUbo(ref MeshUniformBufferObject ubo, in Position.Ref position, in Rotation.Ref rotation, in Scale.Ref scale)
		{
			var s = new Vector3(scale.x, scale.y, scale.z);

			ubo.translation = Matrix4x4.CreateTranslation(new Vector3(position.x, position.y, position.z));
			ubo.rotation = Matrix4x4.CreateFromQuaternion(new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w));
			ubo.scale = Matrix4x4.CreateScale(new Vector3(scale.x, scale.y, scale.z));
		}
	}

	static class GizmoMeshes
	{
		public static Mesh Arrow
			=> Mesh.LoadGltf("Arrow", "Models/Gizmos/Arrow/Arrow.gltf");
	}
}
