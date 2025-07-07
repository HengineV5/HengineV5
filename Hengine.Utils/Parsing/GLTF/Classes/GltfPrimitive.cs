namespace Hengine.Utils.Parsing.GLTF
{
    public class GltfPrimitive
    {
        public GltfAttributes attributes;
        public uint indices;
        public uint material;
        public GltfPrimitiveMode mode = GltfPrimitiveMode.Triangle;
    }
}
