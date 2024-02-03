namespace Engine.Parsing.Gltf
{
    public class GltfFile
    {
        public GltfAsset asset;
        public GltfBuffer[] buffers;
        public GltfBufferView[] bufferViews;
        public uint scene;
        public GltfScene[] scenes;
        public GltfNode[] nodes;
        public GltfMesh[] meshes;
        public GltfAccessor[] accessors;
        public GltfMaterial[] materials;
        public GltfTexture[] textures;
        public GltfSampler[] sampleres;
        public GltfImage[] images;
    }
}
