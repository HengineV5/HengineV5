using Engine.Graphics;
using Silk.NET.Vulkan;
using System.Runtime.CompilerServices;

namespace Engine
{
	static class VulkanRenderHelpers
	{
		public static unsafe void UpdateGuiDescriptorSet(VkContext context, DescriptorSet descriptorSet, VkTextureBuffer textureMap, Sampler sampler)
		{
			Span<DescriptorImageInfo> infos = stackalloc DescriptorImageInfo[1];
			Span<WriteDescriptorSet> descriptorWrites = stackalloc WriteDescriptorSet[1];

			CreateDescriptorWrite(ref infos[0], ref descriptorWrites[0], 1, textureMap, sampler, descriptorSet);

			context.vk.UpdateDescriptorSets(context.device, descriptorWrites, 0, null);
		}

		public static unsafe void UpdateMeshDescriptorSet(VkContext context, DescriptorSet descriptorSet, VkTextureBuffer texture, VkPbrMaterial material, VkSkybox skybox, Span<Sampler> samplers)
		{
			Span<DescriptorImageInfo> infos = stackalloc DescriptorImageInfo[9];
			Span<WriteDescriptorSet> descriptorWrites = stackalloc WriteDescriptorSet[9];

			CreateDescriptorWrite(ref infos[0], ref descriptorWrites[0], 1, texture, samplers[0], descriptorSet);
			CreateDescriptorWrite(ref infos[1], ref descriptorWrites[1], 2, material.albedoMap, samplers[1], descriptorSet);
			CreateDescriptorWrite(ref infos[2], ref descriptorWrites[2], 3, material.normalMap, samplers[2], descriptorSet);
			CreateDescriptorWrite(ref infos[3], ref descriptorWrites[3], 4, material.metallicMap, samplers[3], descriptorSet);
			CreateDescriptorWrite(ref infos[4], ref descriptorWrites[4], 5, material.roughnessMap, samplers[4], descriptorSet);
			CreateDescriptorWrite(ref infos[4], ref descriptorWrites[5], 6, material.depthMap, samplers[5], descriptorSet);
			CreateDescriptorWrite(ref infos[5], ref descriptorWrites[6], 7, skybox.skybox, samplers[6], descriptorSet);
			CreateDescriptorWrite(ref infos[6], ref descriptorWrites[7], 8, skybox.irradiance, samplers[7], descriptorSet);
			CreateDescriptorWrite(ref infos[7], ref descriptorWrites[8], 9, skybox.specular, samplers[8], descriptorSet);

			context.vk.UpdateDescriptorSets(context.device, descriptorWrites, 0, null);
		}

		public static unsafe void UpdateSkyboxDescriptorSet(VkContext context, DescriptorSet descriptorSet, VkTextureBuffer skybox, Span<Sampler> samplers)
		{
			Span<DescriptorImageInfo> infos = stackalloc DescriptorImageInfo[1];
			Span<WriteDescriptorSet> descriptorWrites = stackalloc WriteDescriptorSet[1];

			CreateDescriptorWrite(ref infos[0], ref descriptorWrites[0], 6, skybox, samplers[5], descriptorSet);

			context.vk.UpdateDescriptorSets(context.device, descriptorWrites, 0, null);
		}

		static unsafe void CreateDescriptorWrite(ref DescriptorImageInfo imageInfo, ref WriteDescriptorSet imageDescriptorWrite, uint binding, VkTextureBuffer textureBuffer, Sampler sampler, DescriptorSet descriptorSet)
		{
			imageInfo.ImageLayout = ImageLayout.ReadOnlyOptimal;
			imageInfo.ImageView = textureBuffer.textureImageView;
			imageInfo.Sampler = sampler;

			imageDescriptorWrite.SType = StructureType.WriteDescriptorSet;
			imageDescriptorWrite.DstSet = descriptorSet;
			imageDescriptorWrite.DstBinding = binding;
			imageDescriptorWrite.DstArrayElement = 0;
			imageDescriptorWrite.DescriptorType = DescriptorType.CombinedImageSampler;
			imageDescriptorWrite.DescriptorCount = 1;
			imageDescriptorWrite.PImageInfo = (DescriptorImageInfo*)Unsafe.AsPointer(ref imageInfo);
		}
	}
}
