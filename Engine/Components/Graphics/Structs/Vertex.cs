using RenderLib;

namespace Engine.Graphics
{
	public interface IVertex
	{
		static abstract uint SizeInBytes { get; }

		static abstract VertexLayout Layout { get; }
	}

	public struct Vertex : IVertex
	{
		public static uint SizeInBytes => sizeof(float) * 11; // Byte size of vertex

		public static VertexLayout Layout { get; } = new(SizeInBytes, new VertexAttribute[]
		{
			new VertexAttribute(0, 0, VertexFormat.Float32x3),
			new VertexAttribute(1, sizeof(float) * 3, VertexFormat.Float32x3),
			new VertexAttribute(2, sizeof(float) * 6, VertexFormat.Float32x2),
			new VertexAttribute(3, sizeof(float) * 8, VertexFormat.Float32x3),
		});

		public Vector3f position;
		public Vector3f normal;
		public Vector2f textureCoordinate;
		public Vector3f tangent;

		public Vertex(Vector3f position, Vector3f normal, Vector2f textureCoordinate, Vector3f tangent)
		{
			this.position = position;
			this.normal = normal;
			this.textureCoordinate = textureCoordinate;
			this.tangent = tangent;
		}
	}

	public struct GuiVertex : IVertex
	{
		public static uint SizeInBytes => sizeof(float) * 6 + sizeof(uint) * 1; // Byte size of vertex

		public static VertexLayout Layout { get; } = new(SizeInBytes, new VertexAttribute[]
		{
			new VertexAttribute(0, 0, VertexFormat.Float32x4),
			new VertexAttribute(1, sizeof(float) * 4, VertexFormat.Float32x2),
			new VertexAttribute(2, sizeof(float) * 6, VertexFormat.Uint32),
		});

		public Vector4f position;
		public Vector2f textureCoordinate;
		public uint inverted;

		public GuiVertex(Vector4f position, Vector2f textureCoordinate, bool inverted = false)
		{
			this.position = position;
			this.textureCoordinate = textureCoordinate;
			this.inverted = inverted ? 1u : 0u;
		}
	}

	public struct GizmoVertex : IVertex
	{
		public static uint SizeInBytes => sizeof(float) * 6; // Byte size of vertex

		public static VertexLayout Layout { get; } = new(SizeInBytes, new VertexAttribute[]
		{
			new VertexAttribute(0, 0, VertexFormat.Float32x3),
			new VertexAttribute(1, sizeof(float) * 3, VertexFormat.Float32x3),
		});

		public Vector3f position;
		public Vector3f normal;

		public GizmoVertex(Vector3f position, Vector3f normal)
		{
			this.position = position;
			this.normal = normal;
		}
	}
}
