using EnCS.Attributes;
using Engine.Components;
using Engine.Graphics;
using Engine.Utils;
using MathLib;
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
		public void BufferUpdate(ref VulkanRenderContext context, ref GizmoLine gizmoComp)
		{
			Vector3f a = new(gizmoComp.p1.x, gizmoComp.p1.y, gizmoComp.p1.z);
			Vector3f b = new(gizmoComp.p2.x, gizmoComp.p2.y, gizmoComp.p2.z);

			Vector3f ab = b - a;

			context.gizmoUbo.translation = Matrix4x4f.CreateTranslation(a);
			context.gizmoUbo.rotation = Matrix4x4f.FromQuaternion(MathHelpers.RotateTo<Float32, Quaternion_Ops_Generic<Float32>, Vector3_Ops_Generic<Float32>>(Vector3f.UnitX, Vector3f.Normalize(ab)));
			context.gizmoUbo.scale = Matrix4x4f.CreateScale(Vector3f.One * Vector3f.Length(ab));

			ref GizmoShaderInput shaderInput = ref renderContext.pipeline.GetUbo<GizmoShaderInput>(bufferIdx);
			shaderInput.ubo.Value = context.gizmoUbo;
			shaderInput.gizmoUbo.Value.color = new Vector3f(gizmoComp.color.R, gizmoComp.color.G, gizmoComp.color.B);

			bufferIdx++;
		}

		[SystemUpdate, SystemLayer(0, 2)]
		public void RenderUpdate(ref VulkanRenderContext context, ref GizmoLine gizmoComp)
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
