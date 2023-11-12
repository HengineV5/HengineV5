
using System.Numerics;

namespace Engine.Graphics
{
	public struct Vertex
	{
		public static readonly uint SizeInBytes = sizeof(float) * 8; // Byte size of vertex

		public Vector3 position;
		public Vector3 normal;
		public Vector2 textureCoordinate;

		public Vertex(Vector3 position, Vector3 normal, Vector2 textureCoordinate)
		{
			this.position = position;
			this.normal = normal;
			this.textureCoordinate = textureCoordinate;
		}
	}
}
