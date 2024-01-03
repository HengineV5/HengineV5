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
				data = Image.Load<Rgba32>("Images/image_2.png")
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

		public unsafe VertexBuffer CreateVertexBuffer(GL gl)
		{
			uint vbo = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
			fixed (void* v = &verticies[0])
			{
				gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(verticies.Length * Vertex.SizeInBytes), v, BufferUsageARB.StaticDraw);
			}

			gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Vertex.SizeInBytes, (void*)(0 * Vertex.SizeInBytes));
			gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, Vertex.SizeInBytes, (void*)(3 * sizeof(float)));
			gl.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, Vertex.SizeInBytes, (void*)(6 * sizeof(float)));

			gl.EnableVertexAttribArray(0);
			gl.EnableVertexAttribArray(1);
			gl.EnableVertexAttribArray(2);

			return new VertexBuffer()
			{
				ID = vbo
			};
		}

		public unsafe ElementBuffer CreateElementBuffer(GL gl)
		{
			uint ebo = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
			fixed (void* i = &indicies[0])
			{
				gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indicies.Length * sizeof(uint)), i, BufferUsageARB.StaticDraw);
			}

			return new ElementBuffer()
			{
				ID = ebo
			};
		}

		public static Mesh LoadOBJ(string name, string filePath)
		{
			Mesh mesh = new Mesh()
			{
				name = name,
				indicies = new uint[1024 * 16],
				verticies = new Vertex[1024 * 16]
			};

			using (StreamReader reader = new StreamReader(filePath))
			{
				ObjMeshLoader.Parse(reader, ref mesh);
			}

			return mesh;
		}
	}
}
