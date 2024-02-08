using Engine.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Engine.Parsing.Gltf
{
    [JsonSourceGenerationOptions(IncludeFields = true)]
    [JsonSerializable(typeof(GltfFile))]
    public partial class GltfSourceGenerationContext : JsonSerializerContext
    {

    }

    public static class GltfLoader
    {
        public static void LoadMesh(string name, string path, ref Mesh mesh, bool normalize)
        {
            GltfFile file = JsonSerializer.Deserialize(File.ReadAllText(path), GltfSourceGenerationContext.Default.GltfFile);
            int idx = Array.FindIndex(file.meshes, x => x.name == name);
            ReadMesh(Path.GetDirectoryName(path), file, idx, ref mesh, normalize);
        }

        public static void LoadMaterial(string name, string path, ref PbrMaterial material)
        {
            GltfFile file = JsonSerializer.Deserialize(File.ReadAllText(path), GltfSourceGenerationContext.Default.GltfFile);
            int idx = Array.FindIndex(file.materials, x => x.name == name);
            ReadMaterial(Path.GetDirectoryName(path), file, idx, ref material);
        }

        static void ReadMesh(string baseFolder, GltfFile file, int meshIdx, ref Mesh mesh, bool normalize)
        {
            GltfMesh gltfMesh = file.meshes[meshIdx];
            GltfPrimitive primitive = gltfMesh.primitives[0];

            GltfAccessor normalAccessor = file.accessors[primitive.attributes.normal];
            GltfAccessor positionAccessor = file.accessors[primitive.attributes.position];
            GltfAccessor texAccessor = file.accessors[primitive.attributes.texcoord0];
            GltfAccessor indexAccessor = file.accessors[primitive.indicies];

            Vector3[] normals = new Vector3[normalAccessor.count];
            ReadData(baseFolder, file, normalAccessor, normals.AsSpan());

            Vector3[] positions = new Vector3[positionAccessor.count];
            ReadData(baseFolder, file, positionAccessor, positions.AsSpan());

            if (normalize)
            {
                Vector3 max = new Vector3(positionAccessor.max[0], positionAccessor.max[1], positionAccessor.max[2]);
                Vector3 min = new Vector3(positionAccessor.min[0], positionAccessor.min[1], positionAccessor.min[2]);

                max -= min;
                float maxVal = MathF.Max(max.X, MathF.Max(max.Y, max.Z));

                for (int i = 0; i < positions.Length; i++)
                {
                    positions[i] = ((positions[i] - min) / maxVal) - Vector3.One * 0.5f;
                }
            }

            Vector2[] texcoords = new Vector2[texAccessor.count];
            ReadData(baseFolder, file, texAccessor, texcoords.AsSpan());

            ushort[] indicies = new ushort[indexAccessor.count];
            ReadData(baseFolder, file, indexAccessor, indicies.AsSpan());

            mesh.name = gltfMesh.name;
            mesh.verticies = new Vertex[normals.Length];
            mesh.indicies = new uint[indicies.Length];
            for (int i = 0; i < indicies.Length; i++)
            {
                mesh.indicies[i] = indicies[i];
            }

            for (int i = 0; i < normals.Length; i++)
            {
                mesh.verticies[i] = new Vertex(positions[i], normals[i], texcoords[i]);
            }
        }

        static void ReadMaterial(string baseFolder, GltfFile file, int materialIdx, ref PbrMaterial material)
        {
            GltfMaterial gltfMaterial = file.materials[materialIdx];
            GltfPbrMetallicRoughness pbr = gltfMaterial.pbrMetallicRoughness;

            material.name = gltfMaterial.name;

            if (pbr.baseColorFactor != null && pbr.baseColorFactor.Length >= 3)
                material.albedo = new Vector3(pbr.baseColorFactor[0], pbr.baseColorFactor[1], pbr.baseColorFactor[2]);

            if (pbr.baseColorTexture != null)
            {
                string albedoUri = file.images[pbr.baseColorTexture.index].uri;
                material.albedoMap = ETexture.LoadImage($"{gltfMaterial.name}_albedo", Path.Combine(baseFolder, albedoUri));
            }

            material.metallic = pbr.metallicFactor;
            material.roughness = pbr.roughnessFactor;

            if (pbr.metallicRoughnessTexture != null)
            {
                string metallicUri = file.images[pbr.metallicRoughnessTexture.index].uri;
                material.metallicMap = ETexture.LoadImage($"{gltfMaterial.name}_metallic", Path.Combine(baseFolder, metallicUri));
                material.roughnessMap = ETexture.LoadImage($"{gltfMaterial.name}_roughness", Path.Combine(baseFolder, file.images[pbr.metallicRoughnessTexture.index].uri));
            }

            if (gltfMaterial.occlusionTexture != null)
            {
                string aoUri = file.images[gltfMaterial.occlusionTexture.index].uri;
                material.aoMap = ETexture.LoadImage($"{gltfMaterial.name}_ao", Path.Combine(baseFolder, aoUri));
            }

            if (gltfMaterial.normalTexture != null)
            {
                string normalUri = file.images[gltfMaterial.normalTexture.index].uri;
                material.normalMap = ETexture.LoadImage($"{gltfMaterial.name}_normal", Path.Combine(baseFolder, normalUri));
            }
        }

        static unsafe void ReadData<T>(string baseFolder, GltfFile file, GltfAccessor accessor, Span<T> data) where T : unmanaged
        {
            GltfBufferView bufferView = file.bufferViews[accessor.bufferView];
            GltfBuffer buffer = file.buffers[bufferView.buffer];

            using BinaryReader bufferReader = new BinaryReader(new FileStream(Path.Combine(baseFolder, buffer.uri), FileMode.Open, FileAccess.Read, FileShare.Read));
            bufferReader.BaseStream.Position = (long)(bufferView.byteOffset + accessor.byteOffset);

            if (bufferView.byteStride == 0 || bufferView.byteStride == sizeof(T))
            {
                bufferReader.Read(MemoryMarshal.AsBytes(data));
            }
            else
            {
                throw new Exception();
            }
        }
    }
}
