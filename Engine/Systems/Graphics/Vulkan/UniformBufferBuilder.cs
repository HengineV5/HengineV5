using Engine.Utils;
using Silk.NET.Vulkan;
using System.Runtime.CompilerServices;

namespace Engine
{
	public class UniformBufferBuilder
	{
		int idx = 0;
		Memory<DescriptorBufferInfo> bufferInfos = new DescriptorBufferInfo[128];
		Memory<WriteDescriptorSet> descriptorWrites = new WriteDescriptorSet[128];

		ulong offset = 0;
		DescriptorSet descriptor;
		Silk.NET.Vulkan.Buffer buffer;


		public UniformBufferBuilder(DescriptorSet descriptor, Silk.NET.Vulkan.Buffer buffer)
        {
            this.descriptor = descriptor;
			this.buffer = buffer;
        }

		public unsafe UniformBufferBuilder Variable<T>(uint binding, uint arrayElement = 0) where T : unmanaged
		{
			DescriptorBufferInfo bufferInfo = new();
			bufferInfo.Buffer = buffer;
			bufferInfo.Offset = offset;
			bufferInfo.Range = (ulong)sizeof(T);

			bufferInfos.Span[idx] = bufferInfo;

			WriteDescriptorSet descriptorWrite = new();
			descriptorWrite.SType = StructureType.WriteDescriptorSet;
			descriptorWrite.DstSet = descriptor;
			descriptorWrite.DstBinding = binding;
			descriptorWrite.DstArrayElement = arrayElement;
			descriptorWrite.DescriptorType = DescriptorType.UniformBuffer;
			descriptorWrite.DescriptorCount = 1;
			fixed(DescriptorBufferInfo* infoPtr = &bufferInfos.Span[idx])
			{
				descriptorWrite.PBufferInfo = infoPtr;
			}

			offset += 64 * ((ulong)sizeof(T) / 64 + 1);
			descriptorWrites.Span[idx] = descriptorWrite;
			idx++;

			return this;
		}

		public UniformBufferBuilder Array<T>(uint binding, uint size) where T : unmanaged
		{
			for (uint i = 0; i < size; i++)
			{
				Variable<T>(binding, i);
			}

			return this;
		}

		public ulong GetSize()
		{
			return offset;
		}

		public ulong GetOffset(uint idx)
		{
			return bufferInfos.Span[(int)idx].Offset;
		}

		public unsafe MappedMemory<T> GetElement<T>(void* dataPtr, uint idx) where T : unmanaged
		{
			return new((T*)Unsafe.Add<byte>(dataPtr, (int)GetOffset(idx)));
		}

		public unsafe void UpdateDescriptorSet(VkContext context)
		{
			context.vk.UpdateDescriptorSets(context.device, descriptorWrites.Span.Slice(0, idx), 0, null);
		}
    }
}
