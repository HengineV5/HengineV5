using EnCS;
using EnCS.Attributes;
using Engine.Components.Graphics;
using Engine.Graphics;
using Silk.NET.OpenGL;

namespace Engine.Graphics
{
	public struct GlMeshBuffer
	{
		public VertexArrayObject vao;
		public VertexBuffer vb;
		public ElementBuffer eb;
	}

	[ResourceManager]
	public partial class OpenGLMeshResourceManager : IResourceManager<Mesh, GlMeshBuffer>
	{
		uint idx = 0;
		Memory<Graphics.Mesh> meshes = new Graphics.Mesh[32];
		Memory<Graphics.GlMeshBuffer> meshBuffers = new Graphics.GlMeshBuffer[32];

		Dictionary<string, uint> meshCache = new Dictionary<string, uint>();

		GL gl;

		public OpenGLMeshResourceManager(GL gl)
		{
			this.gl = gl;
		}

		public ref Graphics.GlMeshBuffer Get(uint id)
		{
			return ref meshBuffers.Span[(int)id];
		}

		public uint Store(in Graphics.Mesh mesh)
		{
            if (meshCache.TryGetValue(mesh.name, out uint id))
				return id;

            meshCache.Add(mesh.name, idx);
			meshes.Span[(int)idx] = mesh;
			meshBuffers.Span[(int)idx] = CreateMeshBuffer(gl, mesh);
			return idx++;
		}

		static GlMeshBuffer CreateMeshBuffer(GL gl, Graphics.Mesh mesh)
		{
			var vao = CreateVertexArray(gl, mesh);
			var vb = CreateVertexBuffer(gl, mesh);
			var eb = CreateElementBuffer(gl, mesh);

			return new GlMeshBuffer()
			{
				vao = vao,
				vb = vb,
				eb = eb
			};
		}

		static VertexArrayObject CreateVertexArray(GL gl, Graphics.Mesh mesh)
		{
			uint vao = gl.GenVertexArray();
			gl.BindVertexArray(vao);

			return new VertexArrayObject()
			{
				ID = vao,
				length = (uint)mesh.indicies.Length
			};
		}

		static unsafe VertexBuffer CreateVertexBuffer(GL gl, Graphics.Mesh mesh)
		{
			uint vbo = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
			fixed (void* v = &mesh.verticies[0])
			{
				gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(mesh.verticies.Length * Vertex.SizeInBytes), v, BufferUsageARB.StaticDraw);
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

		static unsafe ElementBuffer CreateElementBuffer(GL gl, Graphics.Mesh mesh)
		{
			uint ebo = gl.GenBuffer();
			gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
			fixed (void* i = &mesh.indicies[0])
			{
				gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(mesh.indicies.Length * sizeof(uint)), i, BufferUsageARB.StaticDraw);
			}

			return new ElementBuffer()
			{
				ID = ebo
			};
		}
	}
}
