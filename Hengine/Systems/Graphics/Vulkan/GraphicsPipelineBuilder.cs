using Hengine.Graphics;
using Silk.NET.Vulkan;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hengine
{
	public ref struct GraphicsPipelineBuilder
    {
        GraphicsPipelineCreateInfo graphicsCreateInfo;

        // Potentially used by steps;
        VertexInputBindingDescription bindingDescription;
        Memory<VertexInputAttributeDescription> attributeDescription;

		PipelineVertexInputStateCreateInfo vertexInputStateCreateInfo;
        PipelineInputAssemblyStateCreateInfo inputAssemblyStateCreateInfo;
        PipelineViewportStateCreateInfo viewportStateCreateInfo;
        PipelineRasterizationStateCreateInfo rasterizationStateCreateInfo;
		PipelineMultisampleStateCreateInfo multisampleStateCreateInfo;
		PipelineColorBlendAttachmentState colorBlendAttatchment;
		PipelineColorBlendStateCreateInfo colorBlendStateCreateInfo;
        Memory<DynamicState> dynamicState;
		PipelineDynamicStateCreateInfo dynamicStateCreateInfo;
		PipelineDepthStencilStateCreateInfo depthStencilStateCreateInfo;

		public GraphicsPipelineBuilder()
        {
            graphicsCreateInfo = new();
			vertexInputStateCreateInfo = new();
			inputAssemblyStateCreateInfo = new();
			viewportStateCreateInfo = new();
			rasterizationStateCreateInfo = new();
			multisampleStateCreateInfo = new();
			colorBlendAttatchment = new();
			colorBlendStateCreateInfo = new();
			dynamicStateCreateInfo = new();
			depthStencilStateCreateInfo = new();
		}

        public unsafe GraphicsPipelineBuilder WithVertexInput(VertexInputBindingDescription bindingDescription, Memory<VertexInputAttributeDescription> attributeDescription)
        {
			this.bindingDescription = bindingDescription;
			this.attributeDescription = attributeDescription;

			vertexInputStateCreateInfo.SType = StructureType.PipelineVertexInputStateCreateInfo;
			vertexInputStateCreateInfo.VertexBindingDescriptionCount = 1;
			vertexInputStateCreateInfo.PVertexBindingDescriptions = (VertexInputBindingDescription*)Unsafe.AsPointer(ref this.bindingDescription);
			vertexInputStateCreateInfo.VertexAttributeDescriptionCount = (uint)this.attributeDescription.Length;
			fixed (VertexInputAttributeDescription* attributeDescriptionPtr = this.attributeDescription.Span)
			{
				vertexInputStateCreateInfo.PVertexAttributeDescriptions = attributeDescriptionPtr;
			}

			graphicsCreateInfo.PVertexInputState = (PipelineVertexInputStateCreateInfo*)Unsafe.AsPointer(ref vertexInputStateCreateInfo);
			return this;
        }

        public unsafe GraphicsPipelineBuilder WithInputAssembly()
        {
			inputAssemblyStateCreateInfo.SType = StructureType.PipelineInputAssemblyStateCreateInfo;
			inputAssemblyStateCreateInfo.Topology = PrimitiveTopology.TriangleList;
			inputAssemblyStateCreateInfo.PrimitiveRestartEnable = false;

			graphicsCreateInfo.PInputAssemblyState = (PipelineInputAssemblyStateCreateInfo*)Unsafe.AsPointer(ref inputAssemblyStateCreateInfo);
            return this;
		}

        public unsafe GraphicsPipelineBuilder WithViewport(Extent2D extent)
        {
			Viewport viewport = new();
			viewport.X = 0;
			viewport.Y = 0;
			viewport.Width = extent.Width;
			viewport.Height = extent.Height;
			viewport.MinDepth = 0;
			viewport.MaxDepth = 1;

			Rect2D scissor = new();
			scissor.Offset = new(0, 0);
			scissor.Extent = extent;

			viewportStateCreateInfo.SType = StructureType.PipelineViewportStateCreateInfo;
			viewportStateCreateInfo.ViewportCount = 1;
			viewportStateCreateInfo.PViewports = &viewport;
			viewportStateCreateInfo.ScissorCount = 1;
			viewportStateCreateInfo.PScissors = &scissor;

			graphicsCreateInfo.PViewportState = (PipelineViewportStateCreateInfo*)Unsafe.AsPointer(ref viewportStateCreateInfo);
			return this;
		}

        public unsafe GraphicsPipelineBuilder WithColorBlend()
        {
			colorBlendAttatchment.ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit;
			colorBlendAttatchment.BlendEnable = false;
			colorBlendAttatchment.SrcColorBlendFactor = BlendFactor.One;
			colorBlendAttatchment.DstColorBlendFactor = BlendFactor.Zero;
			colorBlendAttatchment.ColorBlendOp = BlendOp.Add;
			colorBlendAttatchment.SrcAlphaBlendFactor = BlendFactor.One;
			colorBlendAttatchment.DstAlphaBlendFactor = BlendFactor.One;
			colorBlendAttatchment.AlphaBlendOp = BlendOp.Add;

			colorBlendStateCreateInfo.SType = StructureType.PipelineColorBlendStateCreateInfo;
			colorBlendStateCreateInfo.LogicOpEnable = false;
			colorBlendStateCreateInfo.LogicOp = LogicOp.Copy;
			colorBlendStateCreateInfo.AttachmentCount = 1;
			colorBlendStateCreateInfo.PAttachments = (PipelineColorBlendAttachmentState*)Unsafe.AsPointer(ref colorBlendAttatchment);
			colorBlendStateCreateInfo.BlendConstants[0] = 0;
			colorBlendStateCreateInfo.BlendConstants[1] = 0;
			colorBlendStateCreateInfo.BlendConstants[2] = 0;
			colorBlendStateCreateInfo.BlendConstants[3] = 0;

			graphicsCreateInfo.PColorBlendState = (PipelineColorBlendStateCreateInfo*)Unsafe.AsPointer(ref colorBlendStateCreateInfo);
			return this;
		}

        public unsafe GraphicsPipelineBuilder WithRasterization(PolygonMode polygonMode, CullModeFlags cullMode, float lineWidth)
        {
			rasterizationStateCreateInfo.SType = StructureType.PipelineRasterizationStateCreateInfo;
			rasterizationStateCreateInfo.DepthClampEnable = false;
			rasterizationStateCreateInfo.RasterizerDiscardEnable = false;
			rasterizationStateCreateInfo.PolygonMode = polygonMode;
			rasterizationStateCreateInfo.LineWidth = lineWidth;
			rasterizationStateCreateInfo.CullMode = cullMode;
			rasterizationStateCreateInfo.FrontFace = FrontFace.CounterClockwise;
			rasterizationStateCreateInfo.DepthBiasEnable = false;
			rasterizationStateCreateInfo.DepthBiasConstantFactor = 0;
			rasterizationStateCreateInfo.DepthBiasClamp = 0;
			rasterizationStateCreateInfo.DepthBiasSlopeFactor = 0;

			graphicsCreateInfo.PRasterizationState = (PipelineRasterizationStateCreateInfo*)Unsafe.AsPointer(ref rasterizationStateCreateInfo);
			return this;
		}

        public unsafe GraphicsPipelineBuilder WithMultisample()
        {
			multisampleStateCreateInfo.SType = StructureType.PipelineMultisampleStateCreateInfo;
			multisampleStateCreateInfo.SampleShadingEnable = false;
			multisampleStateCreateInfo.RasterizationSamples = SampleCountFlags.Count1Bit;
			multisampleStateCreateInfo.MinSampleShading = 1;
			multisampleStateCreateInfo.PSampleMask = null;
			multisampleStateCreateInfo.AlphaToCoverageEnable = false;
			multisampleStateCreateInfo.AlphaToOneEnable = false;

			graphicsCreateInfo.PMultisampleState = (PipelineMultisampleStateCreateInfo*)Unsafe.AsPointer(ref multisampleStateCreateInfo);
			return this;
		}

        public unsafe GraphicsPipelineBuilder WithDynamicState(scoped Span<DynamicState> dynamicStates)
        {
			//dynamicState = new DynamicState[2] { DynamicState.Viewport, DynamicState.Scissor };
			dynamicState = new DynamicState[dynamicStates.Length];
			dynamicStates.CopyTo(dynamicState.Span);

			dynamicStateCreateInfo.SType = StructureType.PipelineDynamicStateCreateInfo;
			dynamicStateCreateInfo.DynamicStateCount = 2;
			dynamicStateCreateInfo.PDynamicStates = (DynamicState*)Unsafe.AsPointer(ref dynamicState.Span[0]);

			graphicsCreateInfo.PDynamicState = (PipelineDynamicStateCreateInfo*)Unsafe.AsPointer(ref dynamicStateCreateInfo);
			return this;
		}

        public unsafe GraphicsPipelineBuilder WithDepthStencil(bool depthTest)
        {
			depthStencilStateCreateInfo.SType = StructureType.PipelineDepthStencilStateCreateInfo;
			depthStencilStateCreateInfo.DepthTestEnable = depthTest;
			depthStencilStateCreateInfo.DepthWriteEnable = depthTest;
			depthStencilStateCreateInfo.DepthCompareOp = CompareOp.Less;
			depthStencilStateCreateInfo.DepthBoundsTestEnable = false;
			depthStencilStateCreateInfo.MinDepthBounds = 0.0f;
			depthStencilStateCreateInfo.MaxDepthBounds = 1.0f;
			depthStencilStateCreateInfo.StencilTestEnable = false;
			depthStencilStateCreateInfo.Front = default;
			depthStencilStateCreateInfo.Back = default;

			graphicsCreateInfo.PDepthStencilState = (PipelineDepthStencilStateCreateInfo*)Unsafe.AsPointer(ref depthStencilStateCreateInfo);
			return this;
		}

        public unsafe Pipeline Build(VkContext context, PipelineLayout pipelineLayout, RenderPass renderPass, Shader shader)
        {
			var vertShader = CreateShaderModule(context.vk, shader.Vertex, context.device);
			var fragShader = CreateShaderModule(context.vk, shader.Fragment, context.device);

			Span<PipelineShaderStageCreateInfo> shaderStages = [CreateShaderStage(context, ShaderStageFlags.VertexBit, vertShader), CreateShaderStage(context, ShaderStageFlags.FragmentBit, fragShader)];

			graphicsCreateInfo.SType = StructureType.GraphicsPipelineCreateInfo;
			graphicsCreateInfo.StageCount = (uint)shaderStages.Length;
			fixed (PipelineShaderStageCreateInfo* shaderStagesPtr = shaderStages)
			{
				graphicsCreateInfo.PStages = shaderStagesPtr;
			}

			graphicsCreateInfo.Layout = pipelineLayout;
			graphicsCreateInfo.RenderPass = renderPass;
			graphicsCreateInfo.Subpass = 0;
			graphicsCreateInfo.BasePipelineHandle = default;
			graphicsCreateInfo.BasePipelineIndex = -1;

			var result = context.vk.CreateGraphicsPipelines(context.device, default, 1, graphicsCreateInfo, null, out Pipeline pipeline);
			if (result != Result.Success)
				throw new Exception("Failed to create vkPipelineLayout");

			context.vk.DestroyShaderModule(context.device, vertShader, null);
			context.vk.DestroyShaderModule(context.device, fragShader, null);

            return pipeline;
		}

		static unsafe ShaderModule CreateShaderModule(Vk vk, Memory<byte> code, Device device)
		{
			ShaderModuleCreateInfo createInfo = new();
			createInfo.SType = StructureType.ShaderModuleCreateInfo;
			createInfo.CodeSize = (nuint)code.Length;

			fixed (byte* codePtr = code.Span)
			{
				createInfo.PCode = (uint*)codePtr;
			}

			var result = vk.CreateShaderModule(device, createInfo, null, out ShaderModule shaderModule);
			if (result != Result.Success)
				throw new Exception("Failed to create vkShaderModule");

			return shaderModule;
		}

		static unsafe PipelineShaderStageCreateInfo CreateShaderStage(VkContext context, ShaderStageFlags stage, ShaderModule shaderModule)
		{
			PipelineShaderStageCreateInfo createInfo = new();
			createInfo.SType = StructureType.PipelineShaderStageCreateInfo;
			createInfo.Stage = stage;

			createInfo.Module = shaderModule;
			createInfo.PName = (byte*)Marshal.StringToHGlobalAnsi("main");

			return createInfo;
		}
	}
}
