using System.Runtime.CompilerServices;

using RenderLib;

namespace Engine
{
	public struct RenderPassContainer<TBackend> where TBackend : struct, IRenderBackend<TBackend>, allows ref struct
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

		public static RenderPassContainer<TBackend> Create(ref TBackend backend, TextureFormat colorFormat, TextureFormat depthFormat)
		{
			var skyboxRenderPass = TBackend.CreateRenderPass(ref backend, new RenderPassDesc(
				colorFormat, depthFormat,
				AttachmentLoadOp.Clear, AttachmentLoadOp.Clear,
				AttachmentStoreOp.Store, AttachmentStoreOp.None));

			var meshRenderPass = TBackend.CreateRenderPass(ref backend, new RenderPassDesc(
				colorFormat, depthFormat,
				AttachmentLoadOp.Load, AttachmentLoadOp.Load,
				AttachmentStoreOp.Store, AttachmentStoreOp.Store));

			var guiRenderPass = TBackend.CreateRenderPass(ref backend, new RenderPassDesc(
				colorFormat, depthFormat,
				AttachmentLoadOp.Load, AttachmentLoadOp.Load,
				AttachmentStoreOp.Store, AttachmentStoreOp.None));

			return new RenderPassContainer<TBackend>(skyboxRenderPass, meshRenderPass, guiRenderPass);
		}

		public static void Dispose(ref TBackend backend, ref RenderPassContainer<TBackend> self)
		{
			TBackend.DestroyRenderPass(ref backend, self.skyboxRenderPass);
			TBackend.DestroyRenderPass(ref backend, self.meshRenderPass);
			TBackend.DestroyRenderPass(ref backend, self.guiRenderPass);
		}

		public static GpuRenderPass Get(RenderPassId id, ref RenderPassContainer<TBackend> self)
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
		public static GpuRenderPass GetCompatibleRenderPass(ref RenderPassContainer<TBackend> self)
		 => Get(RenderPassId.Skybox, ref self);
	}
}
