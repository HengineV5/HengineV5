namespace Engine.Utils.Parsing.GLTF
{
    public class GltfNode
    {
        public string name;
        public uint camera;
        public uint[] children;
        public uint skin;
        public float[] matrix;
        public uint mesh;
        public float[] rotation;
        public float[] scale;
        public float[] translation;
    }
}
