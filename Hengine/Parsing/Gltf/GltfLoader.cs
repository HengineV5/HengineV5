using Hengine.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

// TODO: Move file to Utils when components are separated out into own project
namespace Hengine.Utils.Parsing.GLTF
{
    public static class GltfLoader
    {
        public static void LoadMesh(string name, string path, ref Hengine.Graphics.Mesh mesh, bool normalize)
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

        static void ReadMesh(string baseFolder, GltfFile file, int meshIdx, ref Hengine.Graphics.Mesh mesh, bool normalize)
        {
            GltfMesh gltfMesh = file.meshes[meshIdx];
            GltfPrimitive primitive = gltfMesh.primitives[0];

            GltfAccessor normalAccessor = file.accessors[primitive.attributes.normal];
            GltfAccessor positionAccessor = file.accessors[primitive.attributes.position];
            GltfAccessor texAccessor = file.accessors[primitive.attributes.texcoord0];
            GltfAccessor indexAccessor = file.accessors[primitive.indices];

            Vector3f[] normals = new Vector3f[normalAccessor.count];
            ReadData(baseFolder, file, normalAccessor, normals.AsSpan());

            Vector3f[] positions = new Vector3f[positionAccessor.count];
            ReadData(baseFolder, file, positionAccessor, positions.AsSpan());

            if (normalize)
            {
                Vector3f max = new Vector3f(positionAccessor.max[0], positionAccessor.max[1], positionAccessor.max[2]);
                Vector3f min = new Vector3f(positionAccessor.min[0], positionAccessor.min[1], positionAccessor.min[2]);

                max -= min;
                float maxVal = MathF.Max(max.x, MathF.Max(max.y, max.z));

                for (int i = 0; i < positions.Length; i++)
                {
                    positions[i] = ((positions[i] - min) / maxVal) - Vector3f.One * 0.5f;
                }
            }

            Vector2f[] texcoords = new Vector2f[texAccessor.count];
            ReadData(baseFolder, file, texAccessor, texcoords.AsSpan());

            Vector4f[] tangents = [];
            if (primitive.attributes.tangent != uint.MaxValue)
            {
                GltfAccessor tangentAccessor = file.accessors[primitive.attributes.tangent];
                tangents = new Vector4f[tangentAccessor.count];
                ReadData(baseFolder, file, tangentAccessor, tangents.AsSpan());
            }
            else
            {
				tangents = new Vector4f[normals.Length];
				for (int i = 0; i < tangents.Length; i++)
				{
                    tangents[i] = Vector4f.One;
				}
			}

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
                Vector3f tangent = Vector3f.Zero;
                if (i < tangents.Length)
                    tangent = new Vector3f(tangents[i].x, tangents[i].y, tangents[i].z);

                mesh.verticies[i] = new Vertex(positions[i], normals[i], texcoords[i], tangent);
            }
        }

        static void ReadMaterial(string baseFolder, GltfFile file, int materialIdx, ref PbrMaterial material)
        {
            GltfMaterial gltfMaterial = file.materials[materialIdx];
            GltfPbrMetallicRoughness pbr = gltfMaterial.pbrMetallicRoughness;

            material.name = gltfMaterial.name;

            if (pbr.baseColorFactor != null && pbr.baseColorFactor.Length >= 3)
                material.albedo = new Vector3f(pbr.baseColorFactor[0], pbr.baseColorFactor[1], pbr.baseColorFactor[2]);

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
