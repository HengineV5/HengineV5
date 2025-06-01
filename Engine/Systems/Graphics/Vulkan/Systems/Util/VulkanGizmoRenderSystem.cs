using EnCS.Attributes;
using Engine.Components;
using Engine.Graphics;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Linq;
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

		VkMeshBuffer arrow;
		VkMeshBuffer point;

		public VulkanGizmoRenderSystem(VkContext context, VkRenderContext renderContext)
		{
			this.context = context;
			this.renderContext = renderContext;
		}

		public void Init()
		{
			arrow = VulkanMeshResourceManager.CreateGizmoBuffer(context, GizmoMeshes.Arrow);
			point = VulkanMeshResourceManager.CreateGizmoBuffer(context, GizmoMeshes.Point);
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
		public void BufferUpdate(ref VulkanRenderContext context, ref Position position, ref Rotation rotation, ref Scale scale, ref GizmoComp gizmoComp)
		{
			UpdateEntityUbo(ref context.gizmoUbo, ref position, ref rotation, ref scale);

			ref GizmoShaderInput shaderInput = ref renderContext.pipeline.GetUbo<GizmoShaderInput>(bufferIdx);
			shaderInput.ubo.Value = context.gizmoUbo;
			shaderInput.gizmoUbo.Value.color = new Vector3f(gizmoComp.color.R, gizmoComp.color.G, gizmoComp.color.B);

			bufferIdx++;
		}

		[SystemUpdate, SystemLayer(0, 2)]
		public void RenderUpdate(ref VulkanRenderContext context, ref Position position, ref Rotation rotation, ref Scale scale, ref GizmoComp gizmoComp)
		{
			switch (gizmoComp.type)
			{
				case GizmoType.Point:
					renderContext.pipeline.Render(this.context, PipelineContainerLayer.Gizmo, point.vertexBuffer, point.indexBuffer, point.indicies, updateIdx);
					break;
				case GizmoType.Arrow:
					renderContext.pipeline.Render(this.context, PipelineContainerLayer.Gizmo, arrow.vertexBuffer, arrow.indexBuffer, arrow.indicies, updateIdx);
					break;
				default:
					break;
			}

			updateIdx++;
		}

		[SystemPostLoop, SystemLayer(0, 2)]
		public void PostRenderPass()
		{
			renderContext.pipeline.EndRenderPass(context);
		}

		static void UpdateEntityUbo(ref MeshUniformBufferObject ubo, ref Position position, ref Rotation rotation, ref Scale scale)
		{
			ubo.translation = Matrix4x4f.CreateTranslation(new Vector3f(position.x, position.y, position.z));
			ubo.rotation = Matrix4x4f.FromQuaternion(new Quaternionf(rotation.x, rotation.y, rotation.z, rotation.w));
			ubo.scale = Matrix4x4f.CreateScale(new Vector3f(scale.x, scale.y, scale.z));
		}
	}

	static class GizmoMeshes
	{
		public static Mesh Arrow
			=> Mesh.LoadGltf("Arrow", "Models/Gizmos/Arrow/Arrow.gltf");

		public static Mesh Point
			=> Mesh.LoadGltf("Point", "Models/Gizmos/Point/Point.gltf");

		public static Mesh Line
		{
			get
			{
				return new Mesh()
				{
					name = "Line",
					verticies = [
						new Vertex(new(0, 0, 0), Vector3f.Zero, Vector2f.Zero, Vector3f.Zero),
						new Vertex(new(0.5f, 0, 0), Vector3f.Zero, Vector2f.Zero, Vector3f.Zero),
						new Vertex(new(1, 0, 0), Vector3f.Zero, Vector2f.Zero, Vector3f.Zero)
					],
					indicies = [0, 1, 2]
				};
			}
		}
	}
}
