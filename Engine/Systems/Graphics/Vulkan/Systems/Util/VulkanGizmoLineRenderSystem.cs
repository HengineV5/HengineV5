using EnCS.Attributes;
using Engine.Components;
using Engine.Graphics;
using Engine.Utils;
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
	public partial class VulkanGizmoLineRenderSystem
	{
		VkContext context;
		VkRenderContext renderContext;

		VkMeshBuffer line;

		public VulkanGizmoLineRenderSystem(VkContext context, VkRenderContext renderContext)
		{
			this.context = context;
			this.renderContext = renderContext;
		}

		public void Init()
		{
			line = VulkanMeshResourceManager.CreateGizmoBuffer(context, GizmoMeshes.Line);
		}

		// TODO: Refactor out
		int bufferIdx;
		int updateIdx;

		[SystemPreLoop, SystemLayer(0, 2)]
		public void PreRenderPass()
		{
			renderContext.pipeline.StartRenderPass(context, RenderPassId.Mesh, PipelineContainerLayer.GizmoLine);

			bufferIdx = 0;
			updateIdx = 0;
		}

		[SystemUpdate, SystemLayer(0, 2)]
		public void BufferUpdate(ref VulkanRenderContext context, in GizmoLine.Ref gizmoComp)
		{
			Vector3 a = new(gizmoComp.p1.x, gizmoComp.p1.y, gizmoComp.p1.z);
			Vector3 b = new(gizmoComp.p2.x, gizmoComp.p2.y, gizmoComp.p2.z);

			Vector3 ab = b - a;

			context.gizmoUbo.translation = Matrix4x4.CreateTranslation(a);
			context.gizmoUbo.rotation = Matrix4x4.CreateFromQuaternion(QuaternionHelpers.RotateOnto(Vector3.UnitX, ab));
			context.gizmoUbo.scale = Matrix4x4.CreateScale(Vector3.One * ab.Length());

			ref GizmoShaderInput shaderInput = ref renderContext.pipeline.GetUbo<GizmoShaderInput>(bufferIdx);
			shaderInput.ubo.Value = context.gizmoUbo;
			shaderInput.gizmoUbo.Value.color = new Vector3(gizmoComp.color.R, gizmoComp.color.G, gizmoComp.color.B);

			bufferIdx++;
		}

		[SystemUpdate, SystemLayer(0, 2)]
		public void RenderUpdate(ref VulkanRenderContext context, in GizmoLine.Ref gizmoComp)
		{
			renderContext.pipeline.Render(this.context, PipelineContainerLayer.GizmoLine, line.vertexBuffer, line.indexBuffer, line.indicies, updateIdx);

			updateIdx++;
		}

		[SystemPostLoop, SystemLayer(0, 2)]
		public void PostRenderPass()
		{
			renderContext.pipeline.EndRenderPass(context);
		}
	}
}
