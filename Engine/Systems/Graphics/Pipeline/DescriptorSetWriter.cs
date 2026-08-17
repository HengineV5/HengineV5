using Engine.Graphics;
using RenderLib;
using UtilLib.Memory;

namespace Engine
{
	public static class DescriptorSetWriter
	{
		public static void UpdateGuiDescriptorSet<TBackend>(ref TBackend backend, GpuDescriptorSet descriptorSet, TextureBuffer textureMap, GpuSampler sampler)
			where TBackend : struct, IRenderBackend<TBackend>, allows ref struct
		{
			TBackend.WriteTextureBinding(ref backend, descriptorSet, 1, 0, textureMap.texture, sampler);
		}

		public static void UpdateSkyboxDescriptorSet<TBackend>(ref TBackend backend, GpuDescriptorSet descriptorSet, TextureBuffer skybox, in FixedBuffer16<GpuSampler> samplers)
			where TBackend : struct, IRenderBackend<TBackend>, allows ref struct
		{
			TBackend.WriteTextureBinding(ref backend, descriptorSet, 6, 0, skybox.texture, samplers[5]);
		}

		public static void UpdateMeshDescriptorSet<TBackend>(ref TBackend backend, GpuDescriptorSet descriptorSet, TextureBuffer texture, PbrMaterialBuffer material, SkyboxBuffer skybox, in FixedBuffer16<GpuSampler> samplers)
			where TBackend : struct, IRenderBackend<TBackend>, allows ref struct
		{
			TBackend.WriteTextureBinding(ref backend, descriptorSet, 1, 0, texture.texture, samplers[0]);
			TBackend.WriteTextureBinding(ref backend, descriptorSet, 2, 0, material.albedoMap.texture, samplers[1]);
			TBackend.WriteTextureBinding(ref backend, descriptorSet, 3, 0, material.normalMap.texture, samplers[2]);
			TBackend.WriteTextureBinding(ref backend, descriptorSet, 4, 0, material.metallicMap.texture, samplers[3]);
			TBackend.WriteTextureBinding(ref backend, descriptorSet, 5, 0, material.roughnessMap.texture, samplers[4]);
			TBackend.WriteTextureBinding(ref backend, descriptorSet, 6, 0, material.depthMap.texture, samplers[5]);
			TBackend.WriteTextureBinding(ref backend, descriptorSet, 7, 0, skybox.skybox.texture, samplers[6]);
			TBackend.WriteTextureBinding(ref backend, descriptorSet, 8, 0, skybox.irradiance.texture, samplers[7]);
			TBackend.WriteTextureBinding(ref backend, descriptorSet, 9, 0, skybox.specular.texture, samplers[8]);
		}
	}
}
