using EnCS;
using EnCS.Attributes;
using Engine.Components;
using Engine.Graphics;
using Silk.NET.Windowing;

namespace Engine
{
	[System]
	[SystemContext<VulkanRenderContext>]
	[UsingResource<VulkanMeshResourceManager>]
	[UsingResource<VulkanMaterialResourceManager>]
	public partial class VulkanPbrRenderSystem
	{
		private static FixedArray4<Light> defaultLights = new FixedArray4<Light>();

		private static readonly Light defaultLight = new Light
		{
			Ambient = new Vector3f(1f, 1f, 1f),
			Diffuse = new Vector3f(0.5f, 0.5f, 0.5f),
			Specular = new Vector3f(1f, 1f, 1f)
		};

		IWindow window;
		VkContext context;
		VkRenderContext renderContext;

		VkTextureBuffer skyboxHdrTextureBuffer;

		public VulkanPbrRenderSystem(VkContext context, VkRenderContext renderContext, IWindow window)
		{
			this.context = context;
			this.renderContext = renderContext;
			this.window = window;

			defaultLights[0] = defaultLight;
			defaultLights[1] = defaultLight;
			defaultLights[2] = defaultLight;
			defaultLights[3] = defaultLight;

			defaultLights[0].Position = new Vector3f(0, 3, -6);
			defaultLights[1].Position = new Vector3f(10, 5, -2);
			defaultLights[2].Position = new Vector3f(-10, 5, -2);
			defaultLights[3].Position = new Vector3f(0, 10, -2);
		}

		public void Init()
		{
			var textureBrdfLUT = ETextureHdr.LoadImage("BrdfLUT", "Images/Skybox/integrationMap.png");
			skyboxHdrTextureBuffer = VulkanTextureResourceManager.CreateHdrTextureBuffer(context, textureBrdfLUT);
		}

		// TODO: Refactor out
		int bufferIdx;
		int updateIdx;

		[SystemPreLoop, SystemLayer(0, 2)]
		public void PreRenderPass()
		{
			renderContext.pipeline.StartRenderPass(context, RenderPassId.Mesh, PipelineContainerLayer.Pbr);

			bufferIdx = 0;
			updateIdx = 0;
		}

		[SystemUpdate, SystemLayer(0, 2)]
		public void BufferUpdate(ref VulkanRenderContext context, ref Position position, ref Rotation rotation, ref Scale scale, ref VkMeshBuffer mesh, ref VkPbrMaterial material)
		{
			UpdateEntityUbo(ref context.pbrUbo, ref position, ref rotation, ref scale);

			ref PbrShaderInput shaderInput = ref renderContext.pipeline.GetUbo<PbrShaderInput>(bufferIdx);
			shaderInput.ubo.Value = context.pbrUbo;

			shaderInput.material.Value = PbrMaterialInfo.FromMaterial(material);
			for (int i = 0; i < 4; i++)
			{
				shaderInput.lights[i].Value = defaultLights[i];
			}

			VulkanRenderHelpers.UpdateMeshDescriptorSet(this.context, renderContext.pipeline.GetDescriptorSet(PipelineContainerLayer.Pbr, bufferIdx), skyboxHdrTextureBuffer, material, context.skybox, renderContext.samplers);

			bufferIdx++;
		}

		[SystemUpdate, SystemLayer(0, 2)]
		public void RenderUpdate(ref VulkanRenderContext context, ref Position position, ref Rotation rotation, ref Scale scale, ref VkMeshBuffer mesh, ref VkPbrMaterial material)
		{
            renderContext.pipeline.Render(this.context, PipelineContainerLayer.Pbr, mesh.vertexBuffer, mesh.indexBuffer, mesh.indicies, updateIdx);
			updateIdx++;
		}

		[SystemPostLoop, SystemLayer(0, 2)]
		public void PostRenderPass()
		{
			renderContext.pipeline.EndRenderPass(context);
		}

		static void UpdateEntityUbo(ref MeshUniformBufferObject ubo, ref Position position, ref Rotation rotation, ref Scale scale)
		{
			ubo.translation = Matrix4x4f.CreateTranslation(new Vector3f(position.x, position.y, position.z));
			ubo.rotation = Matrix4x4f.FromQuaternion(new Quaternionf(rotation.x, rotation.y, rotation.z, rotation.w));
			ubo.scale = Matrix4x4f.CreateScale(new Vector3f(scale.x, scale.y, scale.z));
		}
	}
}
