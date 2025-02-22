
using System.Numerics;

namespace Engine.Graphics
{
	public struct Vertex
	{
		public static readonly uint SizeInBytes = sizeof(float) * 11; // Byte size of vertex

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

	public struct GuiVertex
	{
		public static readonly uint SizeInBytes = sizeof(float) * 6; // Byte size of vertex

		public Vector4f position;
		public Vector2f textureCoordinate;

		public GuiVertex(Vector4f position, Vector2f textureCoordinate)
		{
			this.position = position;
			this.textureCoordinate = textureCoordinate;
		}
	}

	public struct GizmoVertex
	{
		public static readonly uint SizeInBytes = sizeof(float) * 6; // Byte size of vertex

		public Vector3f position;
		public Vector3f normal;

		public GizmoVertex(Vector3f position, Vector3f normal)
		{
			this.position = position;
			this.normal = normal;
		}
	}
}
