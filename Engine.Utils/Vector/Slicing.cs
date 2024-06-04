using System.Numerics;

namespace Engine.Utils
{
	public static class Slicing
	{
		public static void Slice(scoped Span<Vector2> line, scoped Span<Vector2> verticies, scoped Span<int> indicies, out Memory<Vector2> newVerticies, out Memory<int> newIndicies)
		{
			int idx = 0;
			newVerticies = new Vector2[20];
			newIndicies = new int[0];

			for (int i = 0; i < line.Length - 1; i++)
			{
				if (!IntersectAny(line[i], line[i + 1], verticies, indicies, out int i1, out int i2))
					continue;

				VectorMath.TryGetIntersection(line[i], line[i + 1], verticies[indicies[i1]], verticies[indicies[i2]], 0.001f, out Vector2 p);
				newVerticies.Span[idx++] = p;
            }
		}

		static bool IntersectAny(in Vector2 p1, in Vector2 p2, scoped Span<Vector2> verticies, scoped Span<int> indicies, out int i1, out int i2)
		{
			for (int i = 0; i < indicies.Length; i+=3)
			{
				i1 = i;
				i2 = i + 1;
				if (VectorMath.Intersect(p1, p2, verticies[indicies[i]], verticies[indicies[i + 1]]))
					return true;

				i1 = i + 1;
				i2 = i + 2;
				if (VectorMath.Intersect(p1, p2, verticies[indicies[i + 1]], verticies[indicies[i + 2]]))
					return true;

				i1 = i + 2;
				i2 = i;
				if (VectorMath.Intersect(p1, p2, verticies[indicies[i + 2]], verticies[indicies[i]]))
					return true;
			}

			i1 = 0;
			i2 = 0;
			return false;
		}
	}
}
