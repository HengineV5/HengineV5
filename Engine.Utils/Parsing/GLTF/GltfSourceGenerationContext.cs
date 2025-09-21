using System.Text.Json.Serialization;

namespace Engine.Utils.Parsing.GLTF
{
	[JsonSourceGenerationOptions(IncludeFields = true)]
	[JsonSerializable(typeof(GltfFile))]
	public partial class GltfSourceGenerationContext : JsonSerializerContext
	{

	}
}
