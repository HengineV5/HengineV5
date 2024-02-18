using Engine.Graphics;
using Silk.NET.Vulkan;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Engine
{
    public struct PipelineContainer : IPipelineContainer<PipelineContainer, DefaultPipelineInfo, PipelineContainerLayer>
	{
        struct PushConstant
        {
            public Matrix4x4 model;
        }

        public RenderLayer skyboxLayer;
		public RenderLayer pbrLayer;

        public PipelineContainer(RenderLayer skyboxLayer, RenderLayer pbrLayer)
        {
            this.skyboxLayer = skyboxLayer;
            this.pbrLayer = pbrLayer;
        }

        public static PipelineContainer Create(VkContext context, DescriptorSetLayout descriptorSetLayout, in DefaultPipelineInfo info)
		{
			var pipelineLayout = CreatePipelineLayout(context, descriptorSetLayout);

            var skyboxShader = Shader.FromFiles("Shaders/Pbr/SkyboxVert.spv", "Shaders/Pbr/SkyboxFrag.spv");
			var skyboxPipeline = RenderLayer.CreateSkybox(context, skyboxShader, pipelineLayout, info);

            var pbrShader = Shader.FromFiles("Shaders/Pbr/PbrVert.spv", "Shaders/Pbr/PbrFrag.spv");
            var pbrPipeline = RenderLayer.CreatePbr(context, pbrShader, pipelineLayout, info);

			return new PipelineContainer(skyboxPipeline, pbrPipeline);
		}

        public static Pipeline Get(PipelineContainerLayer layer, ref PipelineContainer self)
        {
			switch (layer)
			{
				case PipelineContainerLayer.Skybox:
                    return self.skyboxLayer.pipeline;
                case PipelineContainerLayer.Pbr:
                    return self.pbrLayer.pipeline;
                default:
                    throw new Exception();
            }
		}

        public static PipelineLayout GetLayout(PipelineContainerLayer layer, ref PipelineContainer self)
        {
            switch (layer)
            {
                case PipelineContainerLayer.Skybox:
                    return self.skyboxLayer.layout;
                case PipelineContainerLayer.Pbr:
                    return self.pbrLayer.layout;
                default:
                    throw new Exception();
            }
        }

        static unsafe PipelineLayout CreatePipelineLayout(VkContext context, DescriptorSetLayout descriptorSetLayout)
        {
            PipelineColorBlendAttachmentState colorBlendAttatchment = new();
            colorBlendAttatchment.ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit;
            colorBlendAttatchment.BlendEnable = false;
            colorBlendAttatchment.SrcColorBlendFactor = BlendFactor.One;
            colorBlendAttatchment.DstColorBlendFactor = BlendFactor.Zero;
            colorBlendAttatchment.ColorBlendOp = BlendOp.Add;
            colorBlendAttatchment.SrcAlphaBlendFactor = BlendFactor.One;
            colorBlendAttatchment.DstAlphaBlendFactor = BlendFactor.One;
            colorBlendAttatchment.AlphaBlendOp = BlendOp.Add;

            PushConstantRange pushConstant = new();
            pushConstant.Offset = 0;
            pushConstant.Size = (uint)sizeof(PushConstant);
            pushConstant.StageFlags = ShaderStageFlags.VertexBit;

            PipelineLayoutCreateInfo pipelineLayoutCreateInfo = new();
            pipelineLayoutCreateInfo.SType = StructureType.PipelineLayoutCreateInfo;
            pipelineLayoutCreateInfo.SetLayoutCount = 1;
            pipelineLayoutCreateInfo.PSetLayouts = &descriptorSetLayout;
            //pipelineLayoutCreateInfo.PushConstantRangeCount = 1;
            //pipelineLayoutCreateInfo.PPushConstantRanges = &pushConstant;

            var result = context.vk.CreatePipelineLayout(context.device, pipelineLayoutCreateInfo, null, out PipelineLayout pipelineLayout);
            if (result != Result.Success)
                throw new Exception("Failed to create vkPipelineLayout");

            return pipelineLayout;
        }
    }

    public struct RenderLayer
    {
        public Shader shader;
        public Pipeline pipeline;
        public PipelineLayout layout;

        public RenderLayer(Shader shader, Pipeline pipeline, PipelineLayout layout)
        {
            this.shader = shader;
            this.pipeline = pipeline;
            this.layout = layout;
        }

        public static RenderLayer CreatePbr(VkContext context, Shader shader, PipelineLayout layout, in DefaultPipelineInfo info)
        {
            return new RenderLayer(shader, CreateGraphicsPipeline(context, info.extent, layout, info.compatibleRenderPass, shader, CullModeFlags.BackBit), layout);
        }

        public static RenderLayer CreateSkybox(VkContext context, Shader shader, PipelineLayout layout, in DefaultPipelineInfo info)
        {
            return new RenderLayer(shader, CreateGraphicsPipeline(context, info.extent, layout, info.compatibleRenderPass, shader, CullModeFlags.FrontBit), layout);
        }

		static unsafe Pipeline CreateGraphicsPipeline(VkContext context, Extent2D extent, PipelineLayout pipelineLayout, RenderPass renderPass, Shader shader, CullModeFlags cullMode)
        {
            var vertShader = CreateShaderModule(context.vk, shader.Vertex, context.device);
            var fragShader = CreateShaderModule(context.vk, shader.Fragment, context.device);

			Span<PipelineShaderStageCreateInfo> shaderStages = [CreateShaderStage(context, ShaderStageFlags.VertexBit, vertShader), CreateShaderStage(context, ShaderStageFlags.FragmentBit, fragShader)];

            DynamicState* dynamicState = stackalloc DynamicState[2] { DynamicState.Viewport, DynamicState.Scissor };

            PipelineDynamicStateCreateInfo dynamicStateCreateInfo = new();
            dynamicStateCreateInfo.SType = StructureType.PipelineDynamicStateCreateInfo;
            dynamicStateCreateInfo.DynamicStateCount = 2;
            dynamicStateCreateInfo.PDynamicStates = &dynamicState[0];

            VertexInputBindingDescription bindingDescription = GetBindingDescription();
            Memory<VertexInputAttributeDescription> attributeDescription = GetAttributeDescription();

            PipelineVertexInputStateCreateInfo vertexInputStateCreateInfo = new();
            vertexInputStateCreateInfo.SType = StructureType.PipelineVertexInputStateCreateInfo;
            vertexInputStateCreateInfo.VertexBindingDescriptionCount = 1;
            vertexInputStateCreateInfo.PVertexBindingDescriptions = &bindingDescription;
            vertexInputStateCreateInfo.VertexAttributeDescriptionCount = (uint)attributeDescription.Length;
            fixed (VertexInputAttributeDescription* attributeDescriptionPtr = attributeDescription.Span)
            {
                vertexInputStateCreateInfo.PVertexAttributeDescriptions = attributeDescriptionPtr;
            }

            PipelineInputAssemblyStateCreateInfo inputAssemblyStateCreateInfo = new();
            inputAssemblyStateCreateInfo.SType = StructureType.PipelineInputAssemblyStateCreateInfo;
            inputAssemblyStateCreateInfo.Topology = PrimitiveTopology.TriangleList;
            inputAssemblyStateCreateInfo.PrimitiveRestartEnable = false;

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

            PipelineViewportStateCreateInfo viewportStateCreateInfo = new();
            viewportStateCreateInfo.SType = StructureType.PipelineViewportStateCreateInfo;
            viewportStateCreateInfo.ViewportCount = 1;
            viewportStateCreateInfo.PViewports = &viewport;
            viewportStateCreateInfo.ScissorCount = 1;
            viewportStateCreateInfo.PScissors = &scissor;

            PipelineRasterizationStateCreateInfo rasterizationStateCreateInfo = new();
            rasterizationStateCreateInfo.SType = StructureType.PipelineRasterizationStateCreateInfo;
            rasterizationStateCreateInfo.DepthClampEnable = false;
            rasterizationStateCreateInfo.RasterizerDiscardEnable = false;
            rasterizationStateCreateInfo.PolygonMode = PolygonMode.Fill;
            rasterizationStateCreateInfo.LineWidth = 1.0f;
            rasterizationStateCreateInfo.CullMode = cullMode;
            rasterizationStateCreateInfo.FrontFace = FrontFace.CounterClockwise;
            rasterizationStateCreateInfo.DepthBiasEnable = false;
            rasterizationStateCreateInfo.DepthBiasConstantFactor = 0;
            rasterizationStateCreateInfo.DepthBiasClamp = 0;
            rasterizationStateCreateInfo.DepthBiasSlopeFactor = 0;

            PipelineMultisampleStateCreateInfo multisampleStateCreateInfo = new();
            multisampleStateCreateInfo.SType = StructureType.PipelineMultisampleStateCreateInfo;
            multisampleStateCreateInfo.SampleShadingEnable = false;
            multisampleStateCreateInfo.RasterizationSamples = SampleCountFlags.Count1Bit;
            multisampleStateCreateInfo.MinSampleShading = 1;
            multisampleStateCreateInfo.PSampleMask = null;
            multisampleStateCreateInfo.AlphaToCoverageEnable = false;
            multisampleStateCreateInfo.AlphaToOneEnable = false;

            PipelineColorBlendAttachmentState colorBlendAttatchment = new();
            colorBlendAttatchment.ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit;
            colorBlendAttatchment.BlendEnable = false;
            colorBlendAttatchment.SrcColorBlendFactor = BlendFactor.One;
            colorBlendAttatchment.DstColorBlendFactor = BlendFactor.Zero;
            colorBlendAttatchment.ColorBlendOp = BlendOp.Add;
            colorBlendAttatchment.SrcAlphaBlendFactor = BlendFactor.One;
            colorBlendAttatchment.DstAlphaBlendFactor = BlendFactor.One;
            colorBlendAttatchment.AlphaBlendOp = BlendOp.Add;

            PipelineColorBlendStateCreateInfo colorBlendStateCreateInfo = new();
            colorBlendStateCreateInfo.SType = StructureType.PipelineColorBlendStateCreateInfo;
            colorBlendStateCreateInfo.LogicOpEnable = false;
            colorBlendStateCreateInfo.LogicOp = LogicOp.Copy;
            colorBlendStateCreateInfo.AttachmentCount = 1;
            colorBlendStateCreateInfo.PAttachments = &colorBlendAttatchment;
            colorBlendStateCreateInfo.BlendConstants[0] = 0;
            colorBlendStateCreateInfo.BlendConstants[1] = 0;
            colorBlendStateCreateInfo.BlendConstants[2] = 0;
            colorBlendStateCreateInfo.BlendConstants[3] = 0;

            PipelineDepthStencilStateCreateInfo depthStencilStateCreateInfo = new();
            depthStencilStateCreateInfo.SType = StructureType.PipelineDepthStencilStateCreateInfo;
            depthStencilStateCreateInfo.DepthTestEnable = true;
            depthStencilStateCreateInfo.DepthWriteEnable = true;
            depthStencilStateCreateInfo.DepthCompareOp = CompareOp.Less;
            depthStencilStateCreateInfo.DepthBoundsTestEnable = false;
            depthStencilStateCreateInfo.MinDepthBounds = 0.0f;
            depthStencilStateCreateInfo.MaxDepthBounds = 1.0f;
            depthStencilStateCreateInfo.StencilTestEnable = false;
            depthStencilStateCreateInfo.Front = default;
            depthStencilStateCreateInfo.Back = default;

            GraphicsPipelineCreateInfo graphicsCreateInfo = new();
            graphicsCreateInfo.SType = StructureType.GraphicsPipelineCreateInfo;
            graphicsCreateInfo.StageCount = (uint)shaderStages.Length;
            fixed (PipelineShaderStageCreateInfo* shaderStagesPtr = shaderStages)
            {
                graphicsCreateInfo.PStages = shaderStagesPtr;
            }

            graphicsCreateInfo.PVertexInputState = &vertexInputStateCreateInfo;
            graphicsCreateInfo.PInputAssemblyState = &inputAssemblyStateCreateInfo;
            graphicsCreateInfo.PViewportState = &viewportStateCreateInfo;
            graphicsCreateInfo.PRasterizationState = &rasterizationStateCreateInfo;
            graphicsCreateInfo.PMultisampleState = &multisampleStateCreateInfo;
            graphicsCreateInfo.PDepthStencilState = null;
            graphicsCreateInfo.PColorBlendState = &colorBlendStateCreateInfo;
            graphicsCreateInfo.PDynamicState = &dynamicStateCreateInfo;
            graphicsCreateInfo.Layout = pipelineLayout;
            graphicsCreateInfo.RenderPass = renderPass;
            graphicsCreateInfo.Subpass = 0;
            graphicsCreateInfo.BasePipelineHandle = default;
            graphicsCreateInfo.BasePipelineIndex = -1;
            graphicsCreateInfo.PDepthStencilState = &depthStencilStateCreateInfo;

            var result = context.vk.CreateGraphicsPipelines(context.device, default, 1, graphicsCreateInfo, null, out Pipeline pipeline);
            if (result != Result.Success)
                throw new Exception("Failed to create vkPipelineLayout");

            context.vk.DestroyShaderModule(context.device, vertShader, null);
            context.vk.DestroyShaderModule(context.device, fragShader, null);

            return pipeline;
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

        static VertexInputBindingDescription GetBindingDescription()
        {
            VertexInputBindingDescription description = new();
            description.Binding = 0;
            description.Stride = Vertex.SizeInBytes;
            description.InputRate = VertexInputRate.Vertex;

            return description;
        }

        static Memory<VertexInputAttributeDescription> GetAttributeDescription()
        {
            Memory<VertexInputAttributeDescription> description = new VertexInputAttributeDescription[4];
            description.Span[0].Binding = 0;
            description.Span[0].Location = 0;
            description.Span[0].Format = Format.R32G32B32Sfloat;
            description.Span[0].Offset = 0;

            description.Span[1].Binding = 0;
            description.Span[1].Location = 1;
            description.Span[1].Format = Format.R32G32B32Sfloat;
            description.Span[1].Offset = sizeof(float) * 3;

            description.Span[2].Binding = 0;
            description.Span[2].Location = 2;
            description.Span[2].Format = Format.R32G32Sfloat;
            description.Span[2].Offset = sizeof(float) * 3 * 2;

			description.Span[3].Binding = 0;
			description.Span[3].Location = 3;
			description.Span[3].Format = Format.R32G32B32Sfloat;
			description.Span[3].Offset = sizeof(float) * 3 * 2 + sizeof(float) * 2;

			return description;
        }
    }
}
