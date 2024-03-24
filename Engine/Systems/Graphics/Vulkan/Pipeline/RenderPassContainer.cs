using Silk.NET.Vulkan;
using System.Runtime.CompilerServices;

namespace Engine
{
	public struct RenderPassContainer : IRenderPassContainer<RenderPassContainer, DefaultRenderPassInfo, RenderPassId>
    {
		public RenderPass skyboxRenderPass;
		public RenderPass meshRenderPass;
        public RenderPass guiRenderPass;

        public RenderPassContainer(RenderPass skyboxRenderPass, RenderPass meshRenderPass, RenderPass guiRenderPass)
        {
            this.skyboxRenderPass = skyboxRenderPass;
            this.meshRenderPass = meshRenderPass;
            this.guiRenderPass = guiRenderPass;
        }

        public static RenderPassContainer Create(VkContext context, in DefaultRenderPassInfo renderPassInfo)
        {
            var skyboxRenderPass = CreateSkyboxRenderPass(context, renderPassInfo.colorFormat, renderPassInfo.depthFormat);
            var meshRenderPass = CreateMeshRenderPass(context, renderPassInfo.colorFormat, renderPassInfo.depthFormat);
            var guiRenderPass = CreateGuiRenderPass(context, renderPassInfo.colorFormat, renderPassInfo.depthFormat);

			return new RenderPassContainer(skyboxRenderPass, meshRenderPass, guiRenderPass);
        }

		public static unsafe void Dispose(VkContext context, ref RenderPassContainer self)
		{
            context.vk.DestroyRenderPass(context.device, self.skyboxRenderPass, null);
            context.vk.DestroyRenderPass(context.device, self.meshRenderPass, null);
		}

		public static RenderPass Get(RenderPassId id, ref RenderPassContainer self)
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
        public static RenderPass GetCompatibleRenderPass(ref RenderPassContainer self)
         => Get(RenderPassId.Skybox, ref self);

		static unsafe RenderPass CreateSkyboxRenderPass(VkContext context, Format colorFormat, Format depthFormat)
        {
            AttachmentDescription colorAttachment = new();
            colorAttachment.Format = colorFormat;
            colorAttachment.Samples = SampleCountFlags.Count1Bit;
            colorAttachment.LoadOp = AttachmentLoadOp.Clear;
            colorAttachment.StoreOp = AttachmentStoreOp.Store;
            colorAttachment.StencilLoadOp = AttachmentLoadOp.DontCare;
            colorAttachment.StencilStoreOp = AttachmentStoreOp.DontCare;
            colorAttachment.InitialLayout = ImageLayout.Undefined;
            colorAttachment.FinalLayout = ImageLayout.PresentSrcKhr;

            AttachmentDescription depthAttatchment = new();
            depthAttatchment.Format = depthFormat;
            depthAttatchment.Samples = SampleCountFlags.Count1Bit;
            depthAttatchment.LoadOp = AttachmentLoadOp.Clear;
            depthAttatchment.StoreOp = AttachmentStoreOp.None;
            depthAttatchment.StencilLoadOp = AttachmentLoadOp.DontCare;
            depthAttatchment.StencilStoreOp = AttachmentStoreOp.DontCare;
            depthAttatchment.InitialLayout = ImageLayout.Undefined;
            depthAttatchment.FinalLayout = ImageLayout.DepthStencilAttachmentOptimal;

            AttachmentReference colorAttachmentRef = new();
            colorAttachmentRef.Attachment = 0;
            colorAttachmentRef.Layout = ImageLayout.ColorAttachmentOptimal;

            AttachmentReference depthAttachmentRef = new();
            depthAttachmentRef.Attachment = 1;
            depthAttachmentRef.Layout = ImageLayout.DepthStencilAttachmentOptimal;

            SubpassDescription subpass = new();
            subpass.PipelineBindPoint = PipelineBindPoint.Graphics;
            subpass.ColorAttachmentCount = 1;
            subpass.PColorAttachments = &colorAttachmentRef;
            subpass.PDepthStencilAttachment = &depthAttachmentRef;

            SubpassDependency dependency = new();
            dependency.SrcSubpass = Vk.SubpassExternal;
            dependency.DstSubpass = 0;
            dependency.SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit;
            dependency.SrcAccessMask = 0;
            dependency.DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit;
            dependency.DstAccessMask = AccessFlags.ColorAttachmentWriteBit | AccessFlags.DepthStencilAttachmentWriteBit;

            AttachmentDescription* attachments = stackalloc AttachmentDescription[2];
            attachments[0] = colorAttachment;
            attachments[1] = depthAttatchment;

            RenderPassCreateInfo createInfo = new();
            createInfo.SType = StructureType.RenderPassCreateInfo;
            createInfo.AttachmentCount = 2;
            createInfo.PAttachments = attachments;
            createInfo.SubpassCount = 1;
            createInfo.PSubpasses = &subpass;
            createInfo.DependencyCount = 1;
            createInfo.PDependencies = &dependency;

            var result = context.vk.CreateRenderPass(context.device, createInfo, null, out RenderPass renderPass);
            if (result != Result.Success)
                throw new Exception("Failed to create vkRenderPass");

            return renderPass;
        }

		static unsafe RenderPass CreateGuiRenderPass(VkContext context, Format colorFormat, Format depthFormat)
		{
			AttachmentDescription colorAttachment = new();
			colorAttachment.Format = colorFormat;
			colorAttachment.Samples = SampleCountFlags.Count1Bit;
			colorAttachment.LoadOp = AttachmentLoadOp.Load;
			colorAttachment.StoreOp = AttachmentStoreOp.Store;
			colorAttachment.StencilLoadOp = AttachmentLoadOp.DontCare;
			colorAttachment.StencilStoreOp = AttachmentStoreOp.DontCare;
			colorAttachment.InitialLayout = ImageLayout.Undefined;
			colorAttachment.FinalLayout = ImageLayout.PresentSrcKhr;

			AttachmentDescription depthAttatchment = new();
			depthAttatchment.Format = depthFormat;
			depthAttatchment.Samples = SampleCountFlags.Count1Bit;
			depthAttatchment.LoadOp = AttachmentLoadOp.Load;
			depthAttatchment.StoreOp = AttachmentStoreOp.None;
			depthAttatchment.StencilLoadOp = AttachmentLoadOp.DontCare;
			depthAttatchment.StencilStoreOp = AttachmentStoreOp.DontCare;
			depthAttatchment.InitialLayout = ImageLayout.Undefined;
			depthAttatchment.FinalLayout = ImageLayout.DepthStencilAttachmentOptimal;

			AttachmentReference colorAttachmentRef = new();
			colorAttachmentRef.Attachment = 0;
			colorAttachmentRef.Layout = ImageLayout.ColorAttachmentOptimal;

			AttachmentReference depthAttachmentRef = new();
			depthAttachmentRef.Attachment = 1;
			depthAttachmentRef.Layout = ImageLayout.DepthStencilAttachmentOptimal;

			SubpassDescription subpass = new();
			subpass.PipelineBindPoint = PipelineBindPoint.Graphics;
			subpass.ColorAttachmentCount = 1;
			subpass.PColorAttachments = &colorAttachmentRef;
			subpass.PDepthStencilAttachment = &depthAttachmentRef;

			SubpassDependency dependency = new();
			dependency.SrcSubpass = Vk.SubpassExternal;
			dependency.DstSubpass = 0;
			dependency.SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit;
			dependency.SrcAccessMask = 0;
			dependency.DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit;
			dependency.DstAccessMask = AccessFlags.ColorAttachmentWriteBit | AccessFlags.DepthStencilAttachmentWriteBit;

			AttachmentDescription* attachments = stackalloc AttachmentDescription[2];
			attachments[0] = colorAttachment;
			attachments[1] = depthAttatchment;

			RenderPassCreateInfo createInfo = new();
			createInfo.SType = StructureType.RenderPassCreateInfo;
			createInfo.AttachmentCount = 2;
			createInfo.PAttachments = attachments;
			createInfo.SubpassCount = 1;
			createInfo.PSubpasses = &subpass;
			createInfo.DependencyCount = 1;
			createInfo.PDependencies = &dependency;

			var result = context.vk.CreateRenderPass(context.device, createInfo, null, out RenderPass renderPass);
			if (result != Result.Success)
				throw new Exception("Failed to create vkRenderPass");

			return renderPass;
		}

		static unsafe RenderPass CreateMeshRenderPass(VkContext context, Format colorFormat, Format depthFormat)
        {
            AttachmentDescription colorAttachment = new();
            colorAttachment.Format = colorFormat;
            colorAttachment.Samples = SampleCountFlags.Count1Bit;
            colorAttachment.LoadOp = AttachmentLoadOp.Load;
            colorAttachment.StoreOp = AttachmentStoreOp.Store;
            colorAttachment.StencilLoadOp = AttachmentLoadOp.DontCare;
            colorAttachment.StencilStoreOp = AttachmentStoreOp.DontCare;
            colorAttachment.InitialLayout = ImageLayout.PresentSrcKhr;
            colorAttachment.FinalLayout = ImageLayout.PresentSrcKhr;

            AttachmentDescription depthAttatchment = new();
            depthAttatchment.Format = depthFormat;
            depthAttatchment.Samples = SampleCountFlags.Count1Bit;
            depthAttatchment.LoadOp = AttachmentLoadOp.Load;
            depthAttatchment.StoreOp = AttachmentStoreOp.Store;
            depthAttatchment.StencilLoadOp = AttachmentLoadOp.DontCare;
            depthAttatchment.StencilStoreOp = AttachmentStoreOp.DontCare;
            depthAttatchment.InitialLayout = ImageLayout.DepthStencilAttachmentOptimal;
            depthAttatchment.FinalLayout = ImageLayout.DepthStencilAttachmentOptimal;

            AttachmentReference colorAttachmentRef = new();
            colorAttachmentRef.Attachment = 0;
            colorAttachmentRef.Layout = ImageLayout.ColorAttachmentOptimal;

            AttachmentReference depthAttachmentRef = new();
            depthAttachmentRef.Attachment = 1;
            depthAttachmentRef.Layout = ImageLayout.DepthStencilAttachmentOptimal;

            SubpassDescription subpass = new();
            subpass.PipelineBindPoint = PipelineBindPoint.Graphics;
            subpass.ColorAttachmentCount = 1;
            subpass.PColorAttachments = &colorAttachmentRef;
            subpass.PDepthStencilAttachment = &depthAttachmentRef;

            SubpassDependency dependency = new();
            dependency.SrcSubpass = Vk.SubpassExternal;
            dependency.DstSubpass = 0;
            dependency.SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit;
            dependency.SrcAccessMask = 0;
            dependency.DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit;
            dependency.DstAccessMask = AccessFlags.ColorAttachmentWriteBit | AccessFlags.DepthStencilAttachmentWriteBit;

            AttachmentDescription* attachments = stackalloc AttachmentDescription[2];
            attachments[0] = colorAttachment;
            attachments[1] = depthAttatchment;

            RenderPassCreateInfo createInfo = new();
            createInfo.SType = StructureType.RenderPassCreateInfo;
            createInfo.AttachmentCount = 2;
            createInfo.PAttachments = attachments;
            createInfo.SubpassCount = 1;
            createInfo.PSubpasses = &subpass;
            createInfo.DependencyCount = 1;
            createInfo.PDependencies = &dependency;

            var result = context.vk.CreateRenderPass(context.device, createInfo, null, out RenderPass renderPass);
            if (result != Result.Success)
                throw new Exception("Failed to create vkRenderPass");

            return renderPass;
        }
	}
}
