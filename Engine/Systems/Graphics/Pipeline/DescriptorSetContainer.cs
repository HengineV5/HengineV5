using RenderLib;

namespace Engine
{
	// TODO: Improve
	internal static class DescriptorSetGroupCache<TDescriptorSet, TStorage>
		where TDescriptorSet : struct, IUniformBufferObject<TDescriptorSet, TStorage>
		where TStorage : struct, IRenderBackend<TStorage>, allows ref struct
	{
		public static bool Initialized = false;
		public static uint size;
		public static Memory<TDescriptorSet> mapped;

		public static ref TDescriptorSet GetMapped(uint frame, uint idx)
		{
			return ref mapped.Span[(int)(frame * size + idx)];
		}
	}

	public struct DescriptorSetGroup<TDescriptorSet, TStorage>
		where TDescriptorSet : struct, IUniformBufferObject<TDescriptorSet, TStorage>
		where TStorage : struct, IRenderBackend<TStorage>, allows ref struct
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

		public static DescriptorSetGroup<TDescriptorSet, TStorage> Create(ref TStorage storage, GpuDescriptorPool pool, GpuDescriptorSetLayout layout, uint frames, uint size)
		{
			var group = new DescriptorSetGroup<TDescriptorSet, TStorage>(new GpuDescriptorSet[frames * size], size);

			if (DescriptorSetGroupCache<TDescriptorSet, TStorage>.Initialized)
				throw new Exception();

			DescriptorSetGroupCache<TDescriptorSet, TStorage>.mapped = new TDescriptorSet[frames * size];
			DescriptorSetGroupCache<TDescriptorSet, TStorage>.size = size;

			for (int i = 0; i < group.descriptorSets.Length; i++)
			{
				GpuDescriptorSet descriptorSet = TStorage.AllocateDescriptorSet(ref storage, pool, layout);

				group.descriptorSets.Span[i] = descriptorSet;
				DescriptorSetGroupCache<TDescriptorSet, TStorage>.mapped.Span[i] = TDescriptorSet.Map(ref storage, descriptorSet);
			}

			DescriptorSetGroupCache<TDescriptorSet, TStorage>.Initialized = true;

			return group;
		}
	}

	public struct DescriptorSetContainer<TStorage> where TStorage : struct, IRenderBackend<TStorage>, allows ref struct
	{
		DescriptorSetGroup<PbrShaderInput<TStorage>, TStorage> pbrDescriptors;
		DescriptorSetGroup<GuiShaderInput<TStorage>, TStorage> guiDescriptors;
		DescriptorSetGroup<GizmoShaderInput<TStorage>, TStorage> gizmoDescriptors;

		GpuDescriptorSetLayout pbrLayout;
		GpuDescriptorSetLayout guiLayout;
		GpuDescriptorSetLayout gizmoLayout;

		public DescriptorSetContainer(DescriptorSetGroup<PbrShaderInput<TStorage>, TStorage> pbrDescriptors, DescriptorSetGroup<GuiShaderInput<TStorage>, TStorage> guiDescriptors, DescriptorSetGroup<GizmoShaderInput<TStorage>, TStorage> gizmoDescriptors, GpuDescriptorSetLayout pbrLayout, GpuDescriptorSetLayout guiLayout, GpuDescriptorSetLayout gizmoLayout)
		{
			this.pbrDescriptors = pbrDescriptors;
			this.guiDescriptors = guiDescriptors;
			this.gizmoDescriptors = gizmoDescriptors;
			this.pbrLayout = pbrLayout;
			this.guiLayout = guiLayout;
			this.gizmoLayout = gizmoLayout;
		}

		public static DescriptorSetContainer<TStorage> Create(ref TStorage storage)
		{
			uint frames = TStorage.GetFramesInFlight(ref storage);
			uint descriptorsPerFrame = 16;

			var pbrLayout = TStorage.CreateDescriptorSetLayout(ref storage, PbrShaderInput<TStorage>.GetLayoutDesc());
			var pbrPool = TStorage.CreateDescriptorPool(ref storage, frames * descriptorsPerFrame);
			var pbr = DescriptorSetGroup<PbrShaderInput<TStorage>, TStorage>.Create(ref storage, pbrPool, pbrLayout, frames, descriptorsPerFrame);

			var guiLayout = TStorage.CreateDescriptorSetLayout(ref storage, GuiShaderInput<TStorage>.GetLayoutDesc());
			var guiPool = TStorage.CreateDescriptorPool(ref storage, frames * descriptorsPerFrame);
			var gui = DescriptorSetGroup<GuiShaderInput<TStorage>, TStorage>.Create(ref storage, guiPool, guiLayout, frames, descriptorsPerFrame);

			var gizmoLayout = TStorage.CreateDescriptorSetLayout(ref storage, GizmoShaderInput<TStorage>.GetLayoutDesc());
			var gizmoPool = TStorage.CreateDescriptorPool(ref storage, frames * descriptorsPerFrame);
			var gizmo = DescriptorSetGroup<GizmoShaderInput<TStorage>, TStorage>.Create(ref storage, gizmoPool, gizmoLayout, frames, descriptorsPerFrame);

			return new DescriptorSetContainer<TStorage>(pbr, gui, gizmo, pbrLayout, guiLayout, gizmoLayout);
		}

		public static GpuDescriptorSet GetDescriptorSet(PipelineContainerLayer layer, uint frame, uint idx, ref DescriptorSetContainer<TStorage> self)
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

		public static GpuDescriptorSetLayout GetDescriptorSetLayout(PipelineContainerLayer layer, ref DescriptorSetContainer<TStorage> self)
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

		public static ref TUbo GetUbo<TUbo>(uint frame, uint idx) where TUbo : struct, IUniformBufferObject<TUbo, TStorage>
		{
			return ref DescriptorSetGroupCache<TUbo, TStorage>.GetMapped(frame, idx);
		}
	}
}
