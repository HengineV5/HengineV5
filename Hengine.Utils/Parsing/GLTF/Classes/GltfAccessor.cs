namespace Hengine.Utils.Parsing.GLTF
{
    public class GltfAccessor
    {
        public string name;
        public uint bufferView;
        public uint byteOffset = 0;
        public GltfComponentType componentType;
        public bool normalized = false;
        public uint count;
        public string type;
        public float[] max;
        public float[] min;
        public GltfAccesorSparse sparse;
    }
}
