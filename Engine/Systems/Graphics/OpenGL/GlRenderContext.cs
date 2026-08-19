using RenderLib;
using RenderLib.OpenGL;
using UtilLib.Memory;

namespace Engine
{
	public class GlRenderContext
	{
		public FixedBuffer16<GpuSampler> samplers;

		public RenderPipeline<GlStorage, HengineRenderGraph<GlStorage>, PipelineContainerLayer, RenderPassId> pipeline;

		GlStorageOwner storageOwner;

		public GlRenderContext(GlContext context)
		{
			this.storageOwner = new GlStorageOwner(context);
		}

		public GlStorage Storage => storageOwner.Storage;

		public void Setup()
		{
			GlStorage storage = storageOwner.Storage;

			samplers = new FixedBuffer16<GpuSampler>();
			for (int i = 0; i < 16; i++)
			{
				samplers[i] = GlStorage.CreateSampler(ref storage, 0);
			}

			samplers[8] = GlStorage.CreateSampler(ref storage, 5);

			pipeline = RenderPipeline<GlStorage, HengineRenderGraph<GlStorage>, PipelineContainerLayer, RenderPassId>.Create(ref storage);
		}
	}
}
