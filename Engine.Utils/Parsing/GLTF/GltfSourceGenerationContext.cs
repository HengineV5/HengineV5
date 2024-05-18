using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Engine.Utils.Parsing.GLTF
{
	[JsonSourceGenerationOptions(IncludeFields = true)]
	[JsonSerializable(typeof(GltfFile))]
	public partial class GltfSourceGenerationContext : JsonSerializerContext
	{

	}
}
