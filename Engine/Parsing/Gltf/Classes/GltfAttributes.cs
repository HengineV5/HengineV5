using System.Text.Json.Serialization;

namespace Engine.Parsing.Gltf
{
    public class GltfAttributes
    {
        [JsonPropertyName("POSITION")]
        public uint position = uint.MaxValue;
        [JsonPropertyName("NORMAL")]
        public uint normal = uint.MaxValue;
        [JsonPropertyName("TEXCOORD_0")]
        public uint texcoord0 = uint.MaxValue;
        [JsonPropertyName("TEXCOORD_1")]
        public uint texcoord1 = uint.MaxValue;
        [JsonPropertyName("TANGENT")]
        public uint tangent = uint.MaxValue;
    }
}
