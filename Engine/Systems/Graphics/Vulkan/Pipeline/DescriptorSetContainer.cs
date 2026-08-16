using Silk.NET.Vulkan;

using RenderLib;
using RenderLib.Vulkan;

namespace Engine
{
	// TODO: Improve
	internal static class DescriptorSetGroupCache<TDescriptorSet> where TDescriptorSet : struct, IUniformBufferObject<TDescriptorSet, VkContext>
	{
		public static bool Initialized = false;
		public static uint size;
		public static Memory<TDescriptorSet> mapped;

		public static ref TDescriptorSet GetMapped(uint frame, uint idx)
		{
			return ref mapped.Span[(int)(frame * size + idx)];
		}
	}

	public struct DescriptorSetGroup<TDescriptorSet> where TDescriptorSet : struct, IUniformBufferObject<TDescriptorSet, VkContext>
	{
		public Memory<DescriptorSet> descriptorSets;
		public uint size;

		public DescriptorSetGroup(Memory<DescriptorSet> descriptorSets, uint size)
		{
			this.descriptorSets = descriptorSets;
			this.size = size;
		}

		public DescriptorSet GetDescriptorSet(uint frame, uint idx)
		{
			return descriptorSets.Span[(int)(frame * size + idx)];
		}

		public static DescriptorSetGroup<TDescriptorSet> Create(VkContext context, DescriptorPool pool, uint frames, uint size)
		{
			DescriptorSetGroup<TDescriptorSet> group = new DescriptorSetGroup<TDescriptorSet>(new DescriptorSet[frames * size], size);

			if (DescriptorSetGroupCache<TDescriptorSet>.Initialized)
				throw new Exception();

			DescriptorSetGroupCache<TDescriptorSet>.mapped = new TDescriptorSet[frames * size];
			DescriptorSetGroupCache<TDescriptorSet>.size = size;

			for (int i = 0; i < group.descriptorSets.Length; i++)
			{
				GpuDescriptorSet gpuDescriptorSet = TDescriptorSet.Create(context, pool.ToGpu());
				group.descriptorSets.Span[i] = gpuDescriptorSet.ToVkDescriptorSet();
				DescriptorSetGroupCache<TDescriptorSet>.mapped.Span[i] = TDescriptorSet.Map(context, gpuDescriptorSet);
			}

			DescriptorSetGroupCache<TDescriptorSet>.Initialized = true;

			return group;
		}
	}

	public struct DescriptorSetContainer : IDescriptorContainer<DescriptorSetContainer, VkContext, PipelineContainerLayer>
	{
		DescriptorSetGroup<PbrShaderInput> pbrDescriptors;
		DescriptorSetGroup<GuiShaderInput> guiDescriptors;
		DescriptorSetGroup<GizmoShaderInput> gizmoDescriptors;

		public DescriptorSetContainer(DescriptorSetGroup<PbrShaderInput> pbrDescriptors, DescriptorSetGroup<GuiShaderInput> guiDescriptors, DescriptorSetGroup<GizmoShaderInput> gizmoDescriptors)
		{
			this.pbrDescriptors = pbrDescriptors;
			this.guiDescriptors = guiDescriptors;
			this.gizmoDescriptors = gizmoDescriptors;
		}

		public static DescriptorSetContainer Create<TRenderTargetManager, TRenderPassInfo, TPipelineInfo>(VkContext context)
			where TRenderTargetManager : struct, IRenderTargetManager<TRenderTargetManager, VkContext, TRenderPassInfo, TPipelineInfo>
			where TRenderPassInfo : struct
			where TPipelineInfo : struct
		{
			uint frames = TRenderTargetManager.GetFramesInFlight();
			uint descriptorsPerFrame = 16;

			var pbrPool = VulkanHelper.CreateDescriptorPool(context, frames * descriptorsPerFrame);
			var pbr = DescriptorSetGroup<PbrShaderInput>.Create(context, pbrPool, frames, descriptorsPerFrame);

			var guiPool = VulkanHelper.CreateDescriptorPool(context, frames * descriptorsPerFrame);
			var gui = DescriptorSetGroup<GuiShaderInput>.Create(context, guiPool, frames, descriptorsPerFrame);

			var gizmoPool = VulkanHelper.CreateDescriptorPool(context, frames * descriptorsPerFrame);
			var gizmo = DescriptorSetGroup<GizmoShaderInput>.Create(context, gizmoPool, frames, descriptorsPerFrame);

			return new DescriptorSetContainer(pbr, gui, gizmo);
		}

		public static GpuDescriptorSet GetDescriptorSet(PipelineContainerLayer layer, uint frame, uint idx, ref DescriptorSetContainer self)
		{
			switch (layer)
			{
				case PipelineContainerLayer.Skybox:
				case PipelineContainerLayer.Pbr:
				case PipelineContainerLayer.Wireframe:
					return self.pbrDescriptors.GetDescriptorSet(frame, idx).ToGpu();
				case PipelineContainerLayer.Gui:
					return self.guiDescriptors.GetDescriptorSet(frame, idx).ToGpu();
				case PipelineContainerLayer.Gizmo:
				case PipelineContainerLayer.GizmoLine:
					return self.gizmoDescriptors.GetDescriptorSet(frame, idx).ToGpu();
				default:
					throw new Exception();
			}
		}

		public static GpuDescriptorSetLayout GetDescriptorSetLayout(VkContext context, PipelineContainerLayer layer)
		{
			switch (layer)
			{
				case PipelineContainerLayer.Skybox:
				case PipelineContainerLayer.Pbr:
				case PipelineContainerLayer.Wireframe:
					return PbrShaderInput.GetLayout(context).ToGpu();
				case PipelineContainerLayer.Gui:
					return GuiShaderInput.GetLayout(context).ToGpu();
				case PipelineContainerLayer.Gizmo:
				case PipelineContainerLayer.GizmoLine:
					return GizmoShaderInput.GetLayout(context).ToGpu();
				default:
					throw new Exception();
			}
		}

		public static ref TUbo GetUbo<TUbo>(uint frame, uint idx) where TUbo : struct, IUniformBufferObject<TUbo, VkContext>
		{
			return ref DescriptorSetGroupCache<TUbo>.GetMapped(frame, idx);
		}
	}
}
