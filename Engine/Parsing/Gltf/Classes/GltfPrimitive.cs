namespace Engine.Parsing.Gltf
{
    public class GltfPrimitive
    {
        public GltfAttributes attributes;
        public uint indicies;
        public uint material;
        public GltfPrimitiveMode mode = GltfPrimitiveMode.Triangle;
    }
}
