
namespace Engine.Graphics
{
	public interface IVertex
	{
		static abstract uint SizeInBytes { get; }
	}

	public struct Vertex : IVertex
	{
		public static uint SizeInBytes => sizeof(float) * 11; // Byte size of vertex

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

		public Vector3f position;
		public Vector3f normal;

		public GizmoVertex(Vector3f position, Vector3f normal)
		{
			this.position = position;
			this.normal = normal;
		}
	}
}
