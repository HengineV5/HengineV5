using Engine.Components.Graphics;
using Engine.Parsing;
using Engine.Parsing.Gltf;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;
using System.Numerics;

namespace Engine.Graphics
{
	public struct ETexture
	{
		public string name;

		public Image<Rgba32> data;

		public static ETexture LoadImage(string name, string filePath)
		{
			return new ETexture()
			{
				name = name,
				data = Image.Load<Rgba32>(filePath)
			};
		}
	}

	public struct ETextureHdr
	{
        public string name;

        public Image<Rgba64> data;

        public static ETextureHdr LoadImage(string name, string filePath)
        {
            return new ETextureHdr()
            {
                name = name,
                data = Image.Load<Rgba64>(filePath)
            };
        }
    }

	public struct ECubemapHdr
	{
        public string name;

        public Image<Rgba64> front;
        public Image<Rgba64> back;
        public Image<Rgba64> left;
        public Image<Rgba64> right;
        public Image<Rgba64> top;
        public Image<Rgba64> bottom;

        public static ECubemapHdr LoadImage(string name, string frontPath, string backPath, string leftPath, string rightPath, string topPath, string bottomPath)
        {
            return new ECubemapHdr()
            {
                name = name,
                front = Image.Load<Rgba64>(frontPath),
                back = Image.Load<Rgba64>(backPath),
                left = Image.Load<Rgba64>(leftPath),
                right = Image.Load<Rgba64>(rightPath),
                top = Image.Load<Rgba64>(topPath),
                bottom = Image.Load<Rgba64>(bottomPath)
            };
        }
    }

	public struct Mesh
	{
		public string name;

		public uint[] indicies;
		public Vertex[] verticies;

		public void RecalculateNormals()
		{
			for (int i = 0; i < indicies.Length / 3; i++)
			{
				uint indexA = indicies[i * 3 + 0];
				uint indexB = indicies[i * 3 + 1];
				uint indexC = indicies[i * 3 + 2];

				Vector3 A = verticies[indexA].position;
				Vector3 AB = verticies[indexB].position - A;
				Vector3 AC = verticies[indexC].position - A;

				if (AB == Vector3.Zero || AC == Vector3.Zero)
					continue;

				Vector3 normal = Vector3.Normalize(Vector3.Cross(AB, AC));
				verticies[indexA].normal = normal;
				verticies[indexB].normal = normal;
				verticies[indexC].normal = normal;
            }
		}

		public VertexArrayObject CreateVertexArray(GL gl)
		{
			uint vao = gl.GenVertexArray();
			gl.BindVertexArray(vao);

			return new VertexArrayObject()
			{
				ID = vao,
				length = (uint)indicies.Length
			};
		}

		public static Mesh LoadOBJ(string name, string filePath)
		{
			Mesh mesh = new Mesh()
			{
				name = name,
				indicies = new uint[1024 * 48],
				verticies = new Vertex[1024 * 48]
			};

			using (StreamReader reader = new StreamReader(filePath))
			{
				ObjMeshLoader.Parse(reader, ref mesh);
			}

			return mesh;
		}

		public static Mesh LoadGltf(string name, string filePath, bool normalize = false)
		{
			Mesh mesh = new Mesh();
			GltfLoader.LoadMesh(name, filePath, ref mesh, normalize);

			return mesh;
		}
	}
}
