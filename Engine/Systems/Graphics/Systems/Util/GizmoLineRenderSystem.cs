using EnCS.Attributes;
using Engine.Components;
using Engine.Graphics;
using MathLib;
using MathLib.Vector.Extensions;

using RenderLib;


namespace Engine
{
	[System]
	[SystemContext<RenderContext>]
	public partial class GizmoLineRenderSystem
	{
		GraphicsContext renderContext;

		MeshBuffer line;

		public GizmoLineRenderSystem(GraphicsContext renderContext)
		{
			this.renderContext = renderContext;
		}

		public void Init()
		{
			var backend = renderContext.CreateBackend();

			line = MeshBufferFactory.CreateGizmoBuffer(ref backend, GizmoMeshes.Line);
		}

		// TODO: Refactor out
		int bufferIdx;
		int updateIdx;

		[SystemPreLoop, SystemLayer(0, 2)]
		public void PreRenderPass()
		{
			var backend = renderContext.CreateBackend();

			renderContext.pipeline.StartRenderPass(ref backend, RenderPassId.Mesh, PipelineContainerLayer.GizmoLine);

			bufferIdx = 0;
			updateIdx = 0;
		}

		[SystemUpdate, SystemLayer(0, 2)]
		public void BufferUpdate(ref RenderContext context, ref GizmoLine gizmoComp)
		{
			var backend = renderContext.CreateBackend();

			Vector3f a = new(gizmoComp.p1.x, gizmoComp.p1.y, gizmoComp.p1.z);
			Vector3f b = new(gizmoComp.p2.x, gizmoComp.p2.y, gizmoComp.p2.z);

			Vector3f ab = b - a;

			context.gizmoUbo.translation = Matrix4x4f.CreateTranslation(a);
			context.gizmoUbo.rotation = Matrix4x4f.FromQuaternion(Quaternionf.CreateRotation(Vector3f.UnitX, Vector3f.Normalize(ab)));
			context.gizmoUbo.scale = Matrix4x4f.CreateScale(Vector3f.One * Vector3f.Length(ab));

			ref GizmoShaderInput<RenderBackend> shaderInput = ref renderContext.pipeline.GetUbo<GizmoShaderInput<RenderBackend>>(ref backend, bufferIdx);
			shaderInput.ubo.Value = context.gizmoUbo;
			shaderInput.gizmoUbo.Value.color = new Vector3f(gizmoComp.color.R, gizmoComp.color.G, gizmoComp.color.B);

			bufferIdx++;
		}

		[SystemUpdate, SystemLayer(0, 2)]
		public void RenderUpdate(ref RenderContext context, ref GizmoLine gizmoComp)
		{
			var backend = renderContext.CreateBackend();

			renderContext.pipeline.Render(ref backend, PipelineContainerLayer.GizmoLine, line.vertexBuffer, line.indexBuffer, line.indicies, updateIdx);

			updateIdx++;
		}

		[SystemPostLoop, SystemLayer(0, 2)]
		public void PostRenderPass()
		{
			var backend = renderContext.CreateBackend();

			renderContext.pipeline.EndRenderPass(ref backend);
		}
	}
}
