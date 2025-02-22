using System.Buffers;

namespace Engine.Utils
{
	public static class Splitting
	{
		struct Triangle
		{
			public int v1;
			public int v2;
			public int v3;

			public Line l1;
			public Line l2;
			public Line l3;

			public Triangle(int v1, int v2, int v3)
			{
				this.v1 = v1;
				this.v2 = v2;
				this.v3 = v3;
			}

			public bool IsSide(int v1, int v2)
			{
				return (this.v1 == v1 && this.v2 == v2) || (this.v2 == v1 && this.v3 == v2) || (this.v3 == v1 && this.v1 == v2);
			}
		}

		struct Line
		{
			public bool onSeam;

			public int v1;
			public int v2;

			public int t1;
			public int t2;

			public Line(bool onSeam, int v1, int v2, int t1, int t2)
			{
				this.onSeam = onSeam;
				this.v1 = v1;
				this.v2 = v2;
				this.t1 = t1;
				this.t2 = t2;
			}

			public int GetOtherNeighbor(int t)
			{
				if (t != t1 && t != t2)
					throw new Exception();

				return t == t2 ? t1 : t2;
			}
		}

		public static void Split(scoped ReadOnlySpan<Vector2f> verticies, scoped ReadOnlySpan<int> indicies, scoped ReadOnlySpan<int> seam, out Memory<Vector2f> verticiesRight, out Memory<int> indiciesRight, out Memory<Vector2f> verticiesLeft, out Memory<int> indiciesLeft)
		{
			using var triMem = MemoryPool<Triangle>.Shared.Rent(indicies.Length / 3);
			SpanList<Triangle> tris = new(triMem.Memory.Span);

			using var rightTriMem = MemoryPool<int>.Shared.Rent(1024);
			SpanList<int> rightTris = new(rightTriMem.Memory.Span);

			using var leftTriMem = MemoryPool<int>.Shared.Rent(1024);
			SpanList<int> leftTris = new(leftTriMem.Memory.Span);

			for (int i = 0; i < indicies.Length; i+=3)
			{
				tris.Add(new Triangle(indicies[i], indicies[i + 1], indicies[i + 2]));
			}

			for (int i = 0; i < tris.Count; i++)
			{
				ref Triangle tri = ref tris[i];

				tri.l1 = GetLine(tri.v1, tri.v2, tris.AsSpan(), seam);
				tri.l2 = GetLine(tri.v2, tri.v3, tris.AsSpan(), seam);
				tri.l3 = GetLine(tri.v3, tri.v1, tris.AsSpan(), seam);
			}

			Line line = GetLine(seam[0], seam[1], tris.AsSpan(), seam);
			/*
            Console.WriteLine(line.t1);
            Console.WriteLine(line.t2);
            Console.WriteLine(line.onSeam);
			*/

			Floodfill(line.t1, tris.AsSpan(), ref rightTris);

            verticiesRight = new();
			indiciesRight = new();
			verticiesLeft = new();
			indiciesLeft = new();
		}

		static Line GetLine(int v1, int v2, scoped ReadOnlySpan<Triangle> tris, scoped ReadOnlySpan<int> seam)
		{
			int t1 = -1;
			int t2 = -1;

			for (int a = 0; a < tris.Length; a++)
			{
				if (tris[a].IsSide(v1, v2))
					t1 = a;

				if (tris[a].IsSide(v2, v1))
					t2 = a;
			}

			bool onSeam = false;
			for (int i = 0; i < seam.Length - 1; i++)
			{
				onSeam = (seam[i] == v1 && seam[i + 1] == v2) || (seam[i] == v2 && seam[i + 1] == v1);

				if (onSeam)
					break;
			}

			return new Line(onSeam, v1, v2, t1, t2);
		}

		static void Floodfill(int startTri, scoped ReadOnlySpan<Triangle> tris, ref SpanList<int> visited)
		{
			static void CheckAndAddNeighbors(int c, ref readonly Line line, scoped ReadOnlySpan<int> neighborsSpan, scoped ReadOnlySpan<int> visitedSpan, ref SpanList<int> neighbors)
			{
				if (line.onSeam)
					return;

				int n = line.GetOtherNeighbor(c);
				if (n != -1 && !visitedSpan.Contains(n) && !neighborsSpan.Contains(n))
					neighbors.Add(n);
			}

			using var neighborsMem = MemoryPool<int>.Shared.Rent(tris.Length);
			SpanList<int> neighbors = new(neighborsMem.Memory.Span);

			neighbors.Add(startTri);
			while (neighbors.Count > 0)
			{
				int current = neighbors[0];
				neighbors.Remove(0);
				visited.Add(current);

				ReadOnlySpan<int> visitedSpan = visited.AsSpan();
				CheckAndAddNeighbors(current, in tris[current].l1, neighbors.AsSpan(), visitedSpan, ref neighbors);
				CheckAndAddNeighbors(current, in tris[current].l2, neighbors.AsSpan(), visitedSpan, ref neighbors);
				CheckAndAddNeighbors(current, in tris[current].l3, neighbors.AsSpan(), visitedSpan, ref neighbors);
			}
		}
	}
}
