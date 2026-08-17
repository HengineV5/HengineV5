using RenderLib;
using RenderLib.OpenGL;
using UtilLib.Memory;

using Backend = RenderLib.Backend;

namespace Engine
{
	public class GlRenderContext
	{
		public FixedBuffer16<GpuSampler> samplers;

		public RenderPipeline<Backend.OpenGL, HengineRenderGraph<Backend.OpenGL>, PipelineContainerLayer, RenderPassId> pipeline;

		GlContext context;
		GlStorageOwner storageOwner;

		public GlRenderContext(GlContext context)
		{
			this.context = context;
			this.storageOwner = new GlStorageOwner(context);
		}

		public Backend.OpenGL CreateBackend()
		{
			return storageOwner.CreateBackend();
		}

		public void Setup()
		{
			Backend.OpenGL backend = storageOwner.CreateBackend();

			samplers = new FixedBuffer16<GpuSampler>();
			for (int i = 0; i < 16; i++)
			{
				samplers[i] = Backend.OpenGL.CreateSampler(ref backend, 0);
			}

			samplers[8] = Backend.OpenGL.CreateSampler(ref backend, 5);

			pipeline = RenderPipeline<Backend.OpenGL, HengineRenderGraph<Backend.OpenGL>, PipelineContainerLayer, RenderPassId>.Create(ref backend);
		}
	}
}
