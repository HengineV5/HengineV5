using EnCS;
using EnCS.Attributes;
using Engine.Components;
using Engine.Graphics;
using Silk.NET.Windowing;
using UtilLib.Memory;

using RenderLib;


namespace Engine
{
	[System]
	[SystemContext<RenderContext>]
	[UsingResource<MeshResourceManager>]
	[UsingResource<MaterialResourceManager>]
	public partial class PbrRenderSystem
	{
		private static FixedBuffer4<Light> defaultLights = new FixedBuffer4<Light>();

		private static readonly Light defaultLight = new Light
		{
			Ambient = new Vector3f(1f, 1f, 1f),
			Diffuse = new Vector3f(0.5f, 0.5f, 0.5f),
			Specular = new Vector3f(1f, 1f, 1f)
		};

		IWindow window;
		GraphicsContext renderContext;

		TextureBuffer skyboxHdrTextureBuffer;

		public PbrRenderSystem(GraphicsContext renderContext, IWindow window)
		{
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
			var backend = renderContext.CreateBackend();

			var textureBrdfLUT = ETextureHdr.LoadImage("BrdfLUT", "Images/Skybox/IntegrationMap.png");
			skyboxHdrTextureBuffer = TextureBufferFactory.CreateHdrTextureBuffer(ref backend, textureBrdfLUT);
		}

		// TODO: Refactor out
		int bufferIdx;
		int updateIdx;

		[SystemPreLoop, SystemLayer(0, 2)]
		public void PreRenderPass()
		{
			var backend = renderContext.CreateBackend();

			renderContext.pipeline.StartRenderPass(ref backend, RenderPassId.Mesh, PipelineContainerLayer.Pbr);

			bufferIdx = 0;
			updateIdx = 0;
		}

		[SystemUpdate, SystemLayer(0, 2)]
		public void BufferUpdate(ref RenderContext context, ref Position position, ref Rotation rotation, ref Scale scale, ref MeshBuffer mesh, ref PbrMaterialBuffer material)
		{
			var backend = renderContext.CreateBackend();

			UpdateEntityUbo(ref context.pbrUbo, ref position, ref rotation, ref scale);

			ref PbrShaderInput<RenderBackend> shaderInput = ref renderContext.pipeline.GetUbo<PbrShaderInput<RenderBackend>>(ref backend, bufferIdx);
			shaderInput.ubo.Value = context.pbrUbo;

			shaderInput.material.Value = PbrMaterialInfo.FromMaterial(material);
			for (int i = 0; i < 4; i++)
			{
				shaderInput.lights[i].Value = defaultLights[i];
			}

			DescriptorSetWriter.UpdateMeshDescriptorSet(ref backend, renderContext.pipeline.GetDescriptorSet(ref backend, PipelineContainerLayer.Pbr, bufferIdx), skyboxHdrTextureBuffer, material, context.skybox, renderContext.samplers);

			bufferIdx++;
		}

		[SystemUpdate, SystemLayer(0, 2)]
		public void RenderUpdate(ref RenderContext context, ref Position position, ref Rotation rotation, ref Scale scale, ref MeshBuffer mesh, ref PbrMaterialBuffer material)
		{
			var backend = renderContext.CreateBackend();

			renderContext.pipeline.Render(ref backend, PipelineContainerLayer.Pbr, mesh.vertexBuffer, mesh.indexBuffer, mesh.indicies, updateIdx);
			updateIdx++;
		}

		[SystemPostLoop, SystemLayer(0, 2)]
		public void PostRenderPass()
		{
			var backend = renderContext.CreateBackend();

			renderContext.pipeline.EndRenderPass(ref backend);
		}

		static void UpdateEntityUbo(ref MeshUniformBufferObject ubo, ref Position position, ref Rotation rotation, ref Scale scale)
		{
			ubo.translation = Matrix4x4f.CreateTranslation(new Vector3f(position.x, position.y, position.z));
			ubo.rotation = Matrix4x4f.FromQuaternion(new Quaternionf(rotation.x, rotation.y, rotation.z, rotation.w));
			ubo.scale = Matrix4x4f.CreateScale(new Vector3f(scale.x, scale.y, scale.z));
		}
	}
}
