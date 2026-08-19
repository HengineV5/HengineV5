using System.Runtime.CompilerServices;

using RenderLib;

namespace Engine
{
	public struct RenderPassContainer<TStorage> where TStorage : struct, IRenderBackend<TStorage>, allows ref struct
	{
		public GpuRenderPass skyboxRenderPass;
		public GpuRenderPass meshRenderPass;
		public GpuRenderPass guiRenderPass;

		public RenderPassContainer(GpuRenderPass skyboxRenderPass, GpuRenderPass meshRenderPass, GpuRenderPass guiRenderPass)
		{
			this.skyboxRenderPass = skyboxRenderPass;
			this.meshRenderPass = meshRenderPass;
			this.guiRenderPass = guiRenderPass;
		}

		public static RenderPassContainer<TStorage> Create(ref TStorage storage, TextureFormat colorFormat, TextureFormat depthFormat)
		{
			var skyboxRenderPass = TStorage.CreateRenderPass(ref storage, new RenderPassDesc(
				colorFormat, depthFormat,
				AttachmentLoadOp.Clear, AttachmentLoadOp.Clear,
				AttachmentStoreOp.Store, AttachmentStoreOp.None));

			var meshRenderPass = TStorage.CreateRenderPass(ref storage, new RenderPassDesc(
				colorFormat, depthFormat,
				AttachmentLoadOp.Load, AttachmentLoadOp.Load,
				AttachmentStoreOp.Store, AttachmentStoreOp.Store));

			var guiRenderPass = TStorage.CreateRenderPass(ref storage, new RenderPassDesc(
				colorFormat, depthFormat,
				AttachmentLoadOp.Load, AttachmentLoadOp.Load,
				AttachmentStoreOp.Store, AttachmentStoreOp.None));

			return new RenderPassContainer<TStorage>(skyboxRenderPass, meshRenderPass, guiRenderPass);
		}

		public static void Dispose(ref TStorage storage, ref RenderPassContainer<TStorage> self)
		{
			TStorage.DestroyRenderPass(ref storage, self.skyboxRenderPass);
			TStorage.DestroyRenderPass(ref storage, self.meshRenderPass);
			TStorage.DestroyRenderPass(ref storage, self.guiRenderPass);
		}

		public static GpuRenderPass Get(RenderPassId id, ref RenderPassContainer<TStorage> self)
		{
			switch (id)
			{
				case RenderPassId.Skybox:
					return self.skyboxRenderPass;
				case RenderPassId.Mesh:
					return self.meshRenderPass;
				case RenderPassId.Gui:
					return self.guiRenderPass;
				default:
					throw new Exception();
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static GpuRenderPass GetCompatibleRenderPass(ref RenderPassContainer<TStorage> self)
		 => Get(RenderPassId.Skybox, ref self);
	}
}
