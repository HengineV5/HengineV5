using Engine.Graphics;
using RenderLib;
using UtilLib.Memory;

namespace Engine
{
	public static class DescriptorSetWriter
	{
		public static void UpdateGuiDescriptorSet<TStorage>(ref TStorage storage, GpuDescriptorSet descriptorSet, TextureBuffer textureMap, GpuSampler sampler)
			where TStorage : struct, IRenderBackend<TStorage>, allows ref struct
		{
			TStorage.WriteTextureBinding(ref storage, descriptorSet, 1, 0, textureMap.texture, sampler);
		}

		public static void UpdateSkyboxDescriptorSet<TStorage>(ref TStorage storage, GpuDescriptorSet descriptorSet, TextureBuffer skybox, in FixedBuffer16<GpuSampler> samplers)
			where TStorage : struct, IRenderBackend<TStorage>, allows ref struct
		{
			TStorage.WriteTextureBinding(ref storage, descriptorSet, 6, 0, skybox.texture, samplers[5]);
		}

		public static void UpdateMeshDescriptorSet<TStorage>(ref TStorage storage, GpuDescriptorSet descriptorSet, TextureBuffer texture, PbrMaterialBuffer material, SkyboxBuffer skybox, in FixedBuffer16<GpuSampler> samplers)
			where TStorage : struct, IRenderBackend<TStorage>, allows ref struct
		{
			TStorage.WriteTextureBinding(ref storage, descriptorSet, 1, 0, texture.texture, samplers[0]);
			TStorage.WriteTextureBinding(ref storage, descriptorSet, 2, 0, material.albedoMap.texture, samplers[1]);
			TStorage.WriteTextureBinding(ref storage, descriptorSet, 3, 0, material.normalMap.texture, samplers[2]);
			TStorage.WriteTextureBinding(ref storage, descriptorSet, 4, 0, material.metallicMap.texture, samplers[3]);
			TStorage.WriteTextureBinding(ref storage, descriptorSet, 5, 0, material.roughnessMap.texture, samplers[4]);
			TStorage.WriteTextureBinding(ref storage, descriptorSet, 6, 0, material.depthMap.texture, samplers[5]);
			TStorage.WriteTextureBinding(ref storage, descriptorSet, 7, 0, skybox.skybox.texture, samplers[6]);
			TStorage.WriteTextureBinding(ref storage, descriptorSet, 8, 0, skybox.irradiance.texture, samplers[7]);
			TStorage.WriteTextureBinding(ref storage, descriptorSet, 9, 0, skybox.specular.texture, samplers[8]);
		}
	}
}
