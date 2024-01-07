using Engine.Components.Graphics;
using Engine.Parsing;
using Silk.NET.OpenGL;
using System.IO;

namespace Engine.Graphics
{
	public partial struct ETexture
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

	public partial struct Mesh
	{
		public string name;

		public uint[] indicies;
		public Vertex[] verticies;

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
	}
}
