using EnCS.Attributes;
using Engine.Components;
using Engine.Graphics;

using RenderLib;


namespace Engine
{
	[System]
	[SystemContext<RenderContext>]
	public partial class GizmoRenderSystem
	{
		GraphicsContext renderContext;

		MeshBuffer arrow;
		MeshBuffer point;

		public GizmoRenderSystem(GraphicsContext renderContext)
		{
			this.renderContext = renderContext;
		}

		public void Init()
		{
			var backend = renderContext.CreateBackend();

			arrow = MeshBufferFactory.CreateGizmoBuffer(ref backend, GizmoMeshes.Arrow);
			point = MeshBufferFactory.CreateGizmoBuffer(ref backend, GizmoMeshes.Point);
		}

		// TODO: Refactor out
		int bufferIdx;
		int updateIdx;

		[SystemPreLoop, SystemLayer(0, 2)]
		public void PreRenderPass()
		{
			var backend = renderContext.CreateBackend();

			renderContext.pipeline.StartRenderPass(ref backend, RenderPassId.Mesh, PipelineContainerLayer.Gizmo);

			bufferIdx = 0;
			updateIdx = 0;
		}

		[SystemUpdate, SystemLayer(0, 2)]
		public void BufferUpdate(ref RenderContext context, ref Position position, ref Rotation rotation, ref Scale scale, ref GizmoComp gizmoComp)
		{
			var backend = renderContext.CreateBackend();

			UpdateEntityUbo(ref context.gizmoUbo, ref position, ref rotation, ref scale);

			ref GizmoShaderInput<RenderBackend> shaderInput = ref renderContext.pipeline.GetUbo<GizmoShaderInput<RenderBackend>>(ref backend, bufferIdx);
			shaderInput.ubo.Value = context.gizmoUbo;
			shaderInput.gizmoUbo.Value.color = new Vector3f(gizmoComp.color.R, gizmoComp.color.G, gizmoComp.color.B);

			bufferIdx++;
		}

		[SystemUpdate, SystemLayer(0, 2)]
		public void RenderUpdate(ref RenderContext context, ref Position position, ref Rotation rotation, ref Scale scale, ref GizmoComp gizmoComp)
		{
			var backend = renderContext.CreateBackend();

			switch (gizmoComp.type)
			{
				case GizmoType.Point:
					renderContext.pipeline.Render(ref backend, PipelineContainerLayer.Gizmo, point.vertexBuffer, point.indexBuffer, point.indicies, updateIdx);
					break;
				case GizmoType.Arrow:
					renderContext.pipeline.Render(ref backend, PipelineContainerLayer.Gizmo, arrow.vertexBuffer, arrow.indexBuffer, arrow.indicies, updateIdx);
					break;
				default:
					break;
			}

			updateIdx++;
		}

		[SystemPostLoop, SystemLayer(0, 2)]
		public void PostRenderPass()
		{
			var backend = renderContext.CreateBackend();

			renderContext.pipeline.EndRenderPass(ref backend);
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
