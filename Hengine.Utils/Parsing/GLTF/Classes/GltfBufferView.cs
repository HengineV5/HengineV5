namespace Hengine.Utils.Parsing.GLTF
{
    public class GltfBufferView
    {
        public string name;
        public uint buffer;
        public ulong byteLength;
        public ulong byteOffset = 0;
        public uint byteStride;
        public GltfBufferViewTarget target;
    }
}
