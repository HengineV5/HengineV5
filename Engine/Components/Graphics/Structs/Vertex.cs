
using System.Numerics;

namespace Engine.Graphics
{
	public struct Vertex
	{
		public static readonly uint SizeInBytes = sizeof(float) * 11; // Byte size of vertex

		public Vector3 position;
		public Vector3 normal;
		public Vector2 textureCoordinate;
		public Vector3 tangent;

		public Vertex(Vector3 position, Vector3 normal, Vector2 textureCoordinate, Vector3 tangent)
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

		public Vector4 position;
		public Vector2 textureCoordinate;

		public GuiVertex(Vector4 position, Vector2 textureCoordinate)
		{
			this.position = position;
			this.textureCoordinate = textureCoordinate;
		}
	}

	public struct GizmoVertex
	{
		public static readonly uint SizeInBytes = sizeof(float) * 6; // Byte size of vertex

		public Vector3 position;
		public Vector3 normal;

		public GizmoVertex(Vector3 position, Vector3 normal)
		{
			this.position = position;
			this.normal = normal;
		}
	}
}
