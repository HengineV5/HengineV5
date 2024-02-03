namespace Engine.Parsing.Gltf
{
    public class GltfMaterial
    {
        public string name;
        public GltfPbrMetallicRoughness pbrMetallicRoughness;
        public GltfNormalTextureInfo normalTexture;
        public GltfOcclusionTextureInfo occlusionTexture;
        public GltfTextureInfo emissiveTexture;
        public float[] emissiveFactor = [0.0f, 0.0f, 0.0f];
        public string alphaMode = "OPAQUE";
        public float alphaCutoff = 0.5f;
        public bool doubleSided = false;
    }
}
