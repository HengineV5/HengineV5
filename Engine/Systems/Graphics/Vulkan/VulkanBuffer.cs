using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Engine
{
    public unsafe ref struct VulkanBuffer
    {
        public Span<byte> Span;

        VkContext context;
        Buffer buffer;
        DeviceMemory bufferMemory;

        public VulkanBuffer(VkContext context, Buffer buffer, DeviceMemory bufferMemory)
        {
            this.context = context;
            this.buffer = buffer;
            this.bufferMemory = bufferMemory;

            Span = VulkanHelper.MapBuffer(context, buffer, bufferMemory);
        }

        public void Unmap()
        {
            VulkanHelper.UnmapBuffer(context, bufferMemory);
        }
    }
}
