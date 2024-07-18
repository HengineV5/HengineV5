using Microsoft.VisualBasic;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Engine.Utils
{
	public static class Slicing
	{
		// Hit is between i1 and i2 with i3 being the third index in the triangle
		// Intersection line goes from o1 to o2
		struct TriangleHit
		{
			public int tri;
			public int face;

			public int i1;
			public int i2;
			public int i3;

			public int o1;
			public int o2;

			public TriangleHit(int tri, int face, int i1, int i2, int i3, int o1, int o2)
			{
				this.tri = tri;
				this.face = face;
				this.i1 = i1;
				this.i2 = i2;
				this.i3 = i3;
				this.o1 = o1;
				this.o2 = o2;
			}

			public bool TryGetPoint(scoped ReadOnlySpan<Vector2> line, scoped ReadOnlySpan<Vector2> verticies, float margin, out Vector2 p)
			{
				return VectorMath.TryGetIntersection(line[o1], line[o2], verticies[i1], verticies[i2], margin, out p);
			}

			public bool TriangleEquals(in TriangleHit triangle)
			{
				return tri == triangle.tri;
			}
		}

		struct TriangleIntersection
		{
			public bool entryClockwise;

			public Vector2 entry;
			public int entryIdx;
			public TriangleHit entryHit;
			public float entryPercent;

			public Vector2 exit;
			public int exitIdx;
			public TriangleHit exitHit;
			public float exitPercent;

			public TriangleIntersection(bool entryClockwise, Vector2 entry, int entryIdx, TriangleHit entryHit, float entryPercent, Vector2 exit, int exitIdx, TriangleHit exitHit, float exitPercent)
			{
				this.entryClockwise = entryClockwise;
				this.entry = entry;
				this.entryIdx = entryIdx;
				this.entryHit = entryHit;
				this.entryPercent = entryPercent;
				this.exit = exit;
				this.exitIdx = exitIdx;
				this.exitHit = exitHit;
				this.exitPercent = exitPercent;
			}

			public static TriangleIntersection Reverse(ref readonly TriangleIntersection inter)
			{
				return new(inter.entryClockwise, inter.exit, inter.exitIdx, inter.exitHit, inter.exitPercent, inter.entry, inter.entryIdx, inter.entryHit, inter.entryPercent);
			}
		}

		struct Intersection
		{
			public Vector2 point;
			public int lineIdx;
			public int vertIdxS;
			public int vertIdxE;
			public int count;

			public Intersection(Vector2 point, int lineIdx, int vertIdxS, int vertIdxE, int count)
			{
				this.point = point;
				this.lineIdx = lineIdx;
				this.vertIdxS = vertIdxS;
				this.vertIdxE = vertIdxE;
				this.count = count;
			}
		}

		public static void SliceNew(scoped Span<Vector2> line, scoped Span<Vector2> verticies, scoped Span<int> indicies, out Memory<Vector2> newVerticies, out Memory<int> newIndicies, float margin = 0.01f)
		{
			using var hitMem = MemoryPool<TriangleHit>.Shared.Rent(indicies.Length);
			SpanList<TriangleHit> hits = new(hitMem.Memory.Span);

			using var intMem = MemoryPool<TriangleIntersection>.Shared.Rent(indicies.Length);
			SpanList<TriangleIntersection> ints = new(intMem.Memory.Span);

			using var vertMem = MemoryPool<Vector2>.Shared.Rent(1024);
			SpanList<Vector2> verts = new(vertMem.Memory.Span);

			using var idxMem = MemoryPool<int>.Shared.Rent(1024);
			SpanList<int> idx = new(idxMem.Memory.Span);

			verts.Add(verticies);
			idx.Add(indicies);

            for (int i = 0; i < line.Length - 1; i++)
			{
				IntersectAny(i, i + 1, line, verticies, indicies, ref hits, margin / 100f);
			}

			for (int i = 0; i < hits.Count - 4; i++)
			{
				ref TriangleHit hit1 = ref hits[i];
				ref TriangleHit hit2 = ref hits[i + 1];
				ref TriangleHit hit3 = ref hits[i + 2];
				ref TriangleHit hit4 = ref hits[i + 3];

				// Detect duplicate hit from line segment ending at intersection.
				if (hit1.tri == hit3.tri && hit2.tri == hit4.tri && hit1.o2 == hit2.o2 && hit3.o1 == hit4.o1 && hit1.o2 == hit3.o1)
				{
					hits.Remove(i + 2);
					hits.Remove(i + 2);

					i -= 2;
                }
			}

            while (hits.Count >= 2)
			{
				GetTriangleIntersection(ref hits, out TriangleIntersection intersection, line, ref verts, margin);
				ints.Add(intersection.entryPercent < intersection.exitPercent ? intersection : TriangleIntersection.Reverse(in intersection)); // Ensure counter clockwise direction
			}

			ints.Sort((int1, int2) => int1.entryHit.tri.CompareTo(int2.entryHit.tri));

			int tri = ints[0].entryHit.tri;
			int prevIdx = 0;
			for (int i = 0; i < ints.Count; i++)
			{
                ref TriangleIntersection inter = ref ints[i];

				if (inter.entryHit.tri != tri)
				{
					ProcessTriangle(line, ints.AsSpan().Slice(prevIdx, i - prevIdx), verticies, indicies, ref verts, ref idx, margin);

					tri = inter.entryHit.tri;
					prevIdx = i;
				}
			}

			ProcessTriangle(line, ints.AsSpan().Slice(prevIdx, ints.Count - prevIdx), verticies, indicies, ref verts, ref idx, margin);

			newVerticies = new Vector2[verts.Count];
			vertMem.Memory.Span.Slice(0, verts.Count).CopyTo(newVerticies.Span);

			newIndicies = new int[idx.Count];
			idxMem.Memory.Span.Slice(0, idx.Count).CopyTo(newIndicies.Span);
		}

		static void ProcessTriangle(scoped Span<Vector2> line, in ReadOnlySpan<TriangleIntersection> intersections, scoped Span<Vector2> verticies, scoped ReadOnlySpan<int> indicies, ref SpanList<Vector2> verts, ref SpanList<int> idx, float margin)
		{
            Span<TriangleIntersection> ints = stackalloc TriangleIntersection[intersections.Length * 2];

			intersections.CopyTo(ints);
			for (int i = 0; i < intersections.Length; i++)
			{
				ints[i + intersections.Length] = TriangleIntersection.Reverse(in intersections[i]);
			}

			ints.Sort((int1, int2) => int1.entryPercent.CompareTo(int2.entryPercent));

			using var tmpIdxMem = MemoryPool<int>.Shared.Rent(1024);
			SpanList<int> tmpIdx = new(tmpIdxMem.Memory.Span);

            TriangulateAndInsert(intersections[0].entryHit.tri, 0, line, ints, indicies, ref verts, ref idx, ref tmpIdx, margin);

			for (int i = 0; i < intersections.Length; i++)
			{
                tmpIdx.Clear();
				TriangulateAndInsert(intersections[0].entryHit.tri, intersections[i].entryPercent, line, ints, indicies, ref verts, ref idx, ref tmpIdx, margin);
			}
		}

		static void CalculateMesh(int tri, float start, scoped Span<Vector2> line, scoped ReadOnlySpan<TriangleIntersection> intersections, scoped ReadOnlySpan<int> indicies, ref SpanList<Vector2> verts, ref SpanList<int> idx, float margin)
		{
			static int GetClosest(float idx, in ReadOnlySpan<TriangleIntersection> intersections)
			{
				for (int i = 0; i < intersections.Length; i++)
				{
					if (intersections[i].entryPercent > idx)
						return i;
				}

				return -1;
			}

			float curr = start;
			while (true)
			{
				int closestIdx = GetClosest(curr, intersections);

                if (closestIdx == -1)
				{
					if (curr < 1)
						idx.Add(indicies[tri * 3 + 1]);

					if (curr < 2)
						idx.Add(indicies[tri * 3 + 2]);

					idx.Add(indicies[tri * 3]);
					break;
				}

				ref readonly TriangleIntersection closest = ref intersections[closestIdx];

				if (curr < 1 && closest.entryPercent > 1)
					idx.Add(indicies[tri * 3 + 1]);

				if (curr < 2 && closest.entryPercent > 2)
					idx.Add(indicies[tri * 3 + 2]);

				idx.Add(closest.entryIdx);

				// Ensure line is added in the correct direction so the mesh is counter clockwise
                if (closest.entryHit.o2 <= closest.exitHit.o2)
				{
					for (int i = closest.entryHit.o2; i < closest.exitHit.o2; i++)
					{
						if (VectorMath.IsClose(line[i], closest.entry, margin) || VectorMath.IsClose(line[i], closest.exit, margin))
							continue;

						verts.Add(line[i]);
						idx.Add(verts.Count - 1);
					}
				}
				else
				{
					for (int i = closest.entryHit.o1; i > closest.exitHit.o1; i--)
					{
						if (VectorMath.IsClose(line[i], closest.entry, margin) || VectorMath.IsClose(line[i], closest.exit, margin))
							continue;

						verts.Add(line[i]);
						idx.Add(verts.Count - 1);
					}
				}

				idx.Add(closest.exitIdx);

				curr = closest.exitPercent;
				if (MathF.Abs(curr - start) < margin)
					break;
			}
		}
		
		static void TriangulateAndInsert(int tri, float start, scoped Span<Vector2> line, scoped Span<TriangleIntersection> ints, scoped ReadOnlySpan<int> indicies, ref SpanList<Vector2> verts, ref SpanList<int> idx, ref SpanList<int> tmpIdx, float margin)
		{
			CalculateMesh(tri, start, line, ints, indicies, ref verts, ref tmpIdx, margin);

			Span<Vector2> tmpVerts = stackalloc Vector2[tmpIdx.Count];
			for (int a = 0; a < tmpVerts.Length; a++)
				tmpVerts[a] = verts[tmpIdx[a]];

			var slicedIndicies = Triangulation.Triangulate(tmpVerts, margin: margin / 100f);
			for (int a = 0; a < slicedIndicies.Length; a++)
			{
				idx.Add(tmpIdx[slicedIndicies.Span[a]]);
			}
		}

		static void GetTriangleIntersection(ref TriangleHit entry, ref TriangleHit exit, out TriangleIntersection intersection, scoped ReadOnlySpan<Vector2> line, ref SpanList<Vector2> verticies, float margin)
		{
			static float GetPercent(Vector2 a, Vector2 b, Vector2 c)
			{
				Vector2 ab = b - a;
				Vector2 ac = c - a;

				return MathF.Sqrt(ac.LengthSquared() / ab.LengthSquared());
			}

			entry.TryGetPoint(line, verticies.AsSpan(), margin, out Vector2 entryPoint);
			exit.TryGetPoint(line, verticies.AsSpan(), margin, out Vector2 exitPoint);

			int entryIdx = verticies.Count;
			verticies.Add(entryPoint);

			int exitIdx = verticies.Count;
			verticies.Add(exitPoint);

			bool entryClockwise = VectorMath.IsClockwise(line[entry.o1], line[entry.o2], verticies[entry.i1]);

			float pEntry = GetPercent(verticies[entry.i1], verticies[entry.i2], entryPoint) + entry.face;
			float pExit = GetPercent(verticies[exit.i1], verticies[exit.i2], exitPoint) + exit.face;

			intersection = new TriangleIntersection(entryClockwise, entryPoint, entryIdx, entry, pEntry, exitPoint, exitIdx, exit, pExit);
		}

		static void GetTriangleIntersection(scoped ref SpanList<TriangleHit> hits, out TriangleIntersection intersection, scoped ReadOnlySpan<Vector2> line, ref SpanList<Vector2> verticies, float margin)
		{
			for (int i = 1; i < hits.Count; i++)
			{
				if (hits[i].tri == hits[0].tri)
				{
					GetTriangleIntersection(ref hits[0], ref hits[i], out intersection, line, ref verticies, margin);

					hits.Remove(i);
					hits.Remove(0);
					return;
				}
			}

			throw new Exception("No matching pair of entry and exit hits.");
		}

		static bool IntersectAny(in int l1, in int l2, scoped ReadOnlySpan<Vector2> line, scoped ReadOnlySpan<Vector2> verticies, scoped ReadOnlySpan<int> indicies, ref SpanList<TriangleHit> hits, float margin)
		{
			// Beware this only works when mesh triangles have a consistent ordering as intersection might not happen on same triangle for entry and exit.
			// As those meshes would not work with culling this is a ok assumption.
			for (int i = 0; i < indicies.Length; i+=3)
			{
				if (VectorMath.Intersect(line[l1], line[l2], verticies[indicies[i]], verticies[indicies[i + 1]], margin))
					hits.Add(new TriangleHit(i / 3, 0, indicies[i], indicies[i + 1], indicies[i + 2], l1, l2));

                if (VectorMath.Intersect(line[l1], line[l2], verticies[indicies[i + 1]], verticies[indicies[i + 2]], margin))
					hits.Add(new TriangleHit(i / 3, 1, indicies[i + 1], indicies[i + 2], indicies[i], l1, l2));

				if (VectorMath.Intersect(line[l1], line[l2], verticies[indicies[i + 2]], verticies[indicies[i]], margin))
					hits.Add(new TriangleHit(i / 3, 2, indicies[i + 2], indicies[i], indicies[i + 1], l1, l2));
			}

			return hits.Count > 0;
		}
	}
}
