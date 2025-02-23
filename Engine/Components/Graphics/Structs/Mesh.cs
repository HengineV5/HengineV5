using Engine.Components.Graphics;
using Engine.Parsing;
using Engine.Utils.Parsing.GLTF;
using ImageLib;
using Silk.NET.OpenGL;
using System.IO;

namespace Engine.Graphics
{
	public struct ETexture
	{
		public string name;

		public ImageMemory<Rgba32> data;

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

        public ImageMemory<Rgba64> data;

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

        public ImageMemory<Rgba64> front;
        public ImageMemory<Rgba64> back;
        public ImageMemory<Rgba64> left;
        public ImageMemory<Rgba64> right;
        public ImageMemory<Rgba64> top;
        public ImageMemory<Rgba64> bottom;

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

				Vector3f A = verticies[indexA].position;
				Vector3f AB = verticies[indexB].position - A;
				Vector3f AC = verticies[indexC].position - A;

				if (AB == Vector3f.Zero || AC == Vector3f.Zero)
					continue;

				Vector3f normal = Vector3f.Normalize(Vector3f.Cross(in AB, in AC));
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

	public struct GuiMesh
	{
		public ushort[] indicies;
		public GuiVertex[] verticies;
	}
}
