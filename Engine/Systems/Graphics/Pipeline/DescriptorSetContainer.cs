using RenderLib;

namespace Engine
{
	// TODO: Improve
	internal static class DescriptorSetGroupCache<TDescriptorSet, TBackend>
		where TDescriptorSet : struct, IUniformBufferObject<TDescriptorSet, TBackend>
		where TBackend : struct, IRenderBackend<TBackend>, allows ref struct
	{
		public static bool Initialized = false;
		public static uint size;
		public static Memory<TDescriptorSet> mapped;

		public static ref TDescriptorSet GetMapped(uint frame, uint idx)
		{
			return ref mapped.Span[(int)(frame * size + idx)];
		}
	}

	public struct DescriptorSetGroup<TDescriptorSet, TBackend>
		where TDescriptorSet : struct, IUniformBufferObject<TDescriptorSet, TBackend>
		where TBackend : struct, IRenderBackend<TBackend>, allows ref struct
	{
		public Memory<GpuDescriptorSet> descriptorSets;
		public uint size;

		public DescriptorSetGroup(Memory<GpuDescriptorSet> descriptorSets, uint size)
		{
			this.descriptorSets = descriptorSets;
			this.size = size;
		}

		public GpuDescriptorSet GetDescriptorSet(uint frame, uint idx)
		{
			return descriptorSets.Span[(int)(frame * size + idx)];
		}

		public static DescriptorSetGroup<TDescriptorSet, TBackend> Create(ref TBackend backend, GpuDescriptorPool pool, GpuDescriptorSetLayout layout, uint frames, uint size)
		{
			var group = new DescriptorSetGroup<TDescriptorSet, TBackend>(new GpuDescriptorSet[frames * size], size);

			if (DescriptorSetGroupCache<TDescriptorSet, TBackend>.Initialized)
				throw new Exception();

			DescriptorSetGroupCache<TDescriptorSet, TBackend>.mapped = new TDescriptorSet[frames * size];
			DescriptorSetGroupCache<TDescriptorSet, TBackend>.size = size;

			for (int i = 0; i < group.descriptorSets.Length; i++)
			{
				GpuDescriptorSet descriptorSet = TBackend.AllocateDescriptorSet(ref backend, pool, layout);

				group.descriptorSets.Span[i] = descriptorSet;
				DescriptorSetGroupCache<TDescriptorSet, TBackend>.mapped.Span[i] = TDescriptorSet.Map(ref backend, descriptorSet);
			}

			DescriptorSetGroupCache<TDescriptorSet, TBackend>.Initialized = true;

			return group;
		}
	}

	public struct DescriptorSetContainer<TBackend> where TBackend : struct, IRenderBackend<TBackend>, allows ref struct
	{
		DescriptorSetGroup<PbrShaderInput<TBackend>, TBackend> pbrDescriptors;
		DescriptorSetGroup<GuiShaderInput<TBackend>, TBackend> guiDescriptors;
		DescriptorSetGroup<GizmoShaderInput<TBackend>, TBackend> gizmoDescriptors;

		GpuDescriptorSetLayout pbrLayout;
		GpuDescriptorSetLayout guiLayout;
		GpuDescriptorSetLayout gizmoLayout;

		public DescriptorSetContainer(DescriptorSetGroup<PbrShaderInput<TBackend>, TBackend> pbrDescriptors, DescriptorSetGroup<GuiShaderInput<TBackend>, TBackend> guiDescriptors, DescriptorSetGroup<GizmoShaderInput<TBackend>, TBackend> gizmoDescriptors, GpuDescriptorSetLayout pbrLayout, GpuDescriptorSetLayout guiLayout, GpuDescriptorSetLayout gizmoLayout)
		{
			this.pbrDescriptors = pbrDescriptors;
			this.guiDescriptors = guiDescriptors;
			this.gizmoDescriptors = gizmoDescriptors;
			this.pbrLayout = pbrLayout;
			this.guiLayout = guiLayout;
			this.gizmoLayout = gizmoLayout;
		}

		public static DescriptorSetContainer<TBackend> Create(ref TBackend backend)
		{
			uint frames = TBackend.GetFramesInFlight(ref backend);
			uint descriptorsPerFrame = 16;

			var pbrLayout = TBackend.CreateDescriptorSetLayout(ref backend, PbrShaderInput<TBackend>.GetLayoutDesc());
			var pbrPool = TBackend.CreateDescriptorPool(ref backend, frames * descriptorsPerFrame);
			var pbr = DescriptorSetGroup<PbrShaderInput<TBackend>, TBackend>.Create(ref backend, pbrPool, pbrLayout, frames, descriptorsPerFrame);

			var guiLayout = TBackend.CreateDescriptorSetLayout(ref backend, GuiShaderInput<TBackend>.GetLayoutDesc());
			var guiPool = TBackend.CreateDescriptorPool(ref backend, frames * descriptorsPerFrame);
			var gui = DescriptorSetGroup<GuiShaderInput<TBackend>, TBackend>.Create(ref backend, guiPool, guiLayout, frames, descriptorsPerFrame);

			var gizmoLayout = TBackend.CreateDescriptorSetLayout(ref backend, GizmoShaderInput<TBackend>.GetLayoutDesc());
			var gizmoPool = TBackend.CreateDescriptorPool(ref backend, frames * descriptorsPerFrame);
			var gizmo = DescriptorSetGroup<GizmoShaderInput<TBackend>, TBackend>.Create(ref backend, gizmoPool, gizmoLayout, frames, descriptorsPerFrame);

			return new DescriptorSetContainer<TBackend>(pbr, gui, gizmo, pbrLayout, guiLayout, gizmoLayout);
		}

		public static GpuDescriptorSet GetDescriptorSet(PipelineContainerLayer layer, uint frame, uint idx, ref DescriptorSetContainer<TBackend> self)
		{
			switch (layer)
			{
				case PipelineContainerLayer.Skybox:
				case PipelineContainerLayer.Pbr:
				case PipelineContainerLayer.Wireframe:
					return self.pbrDescriptors.GetDescriptorSet(frame, idx);
				case PipelineContainerLayer.Gui:
					return self.guiDescriptors.GetDescriptorSet(frame, idx);
				case PipelineContainerLayer.Gizmo:
				case PipelineContainerLayer.GizmoLine:
					return self.gizmoDescriptors.GetDescriptorSet(frame, idx);
				default:
					throw new Exception();
			}
		}

		public static GpuDescriptorSetLayout GetDescriptorSetLayout(PipelineContainerLayer layer, ref DescriptorSetContainer<TBackend> self)
		{
			switch (layer)
			{
				case PipelineContainerLayer.Skybox:
				case PipelineContainerLayer.Pbr:
				case PipelineContainerLayer.Wireframe:
					return self.pbrLayout;
				case PipelineContainerLayer.Gui:
					return self.guiLayout;
				case PipelineContainerLayer.Gizmo:
				case PipelineContainerLayer.GizmoLine:
					return self.gizmoLayout;
				default:
					throw new Exception();
			}
		}

		public static ref TUbo GetUbo<TUbo>(uint frame, uint idx) where TUbo : struct, IUniformBufferObject<TUbo, TBackend>
		{
			return ref DescriptorSetGroupCache<TUbo, TBackend>.GetMapped(frame, idx);
		}
	}
}
