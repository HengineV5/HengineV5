using MathLib;
using System.Buffers;
using System.Runtime.InteropServices;
using UtilLib.Extensions;
using UtilLib.Span;

namespace Engine.Utils
{
	public static class Triangulation
	{
		struct Ear
		{
			public int prev;
			public int curr;
			public int next;
		}

		public static int GetMeshWithHoleSize(scoped ReadOnlySpan<Vector2f> mesh, scoped ReadOnlySpan<Vector2f> hole)
			=> GetMeshWithHoleSize(mesh.Length, hole.Length);

		public static int GetMeshWithHoleSize(int meshVerticeis, int holeVerticies)
			=> meshVerticeis + holeVerticies + 2;

		// Add triangulatable hole into a mesh
		public static Memory<Vector2f> AddHole(scoped ReadOnlySpan<Vector2f> mesh, scoped ReadOnlySpan<Vector2f> hole, float margin = 0.001f)
		{
			int totalVerts = GetMeshWithHoleSize(mesh, hole);
			Memory<Vector2f> dest = new Vector2f[totalVerts];
			AddHole(mesh, hole, dest.Span, margin);
			return dest;
		}

		public static void AddHole(scoped ReadOnlySpan<Vector2f> mesh, scoped ReadOnlySpan<Vector2f> hole, scoped Span<Vector2f> dest, float margin = 0.001f)
		{
			int closest = -1;
			float dist = float.MaxValue;

			for (int h = 0; h < hole.Length - 1; h++)
			{
				ref readonly Vector2f holePoint = ref hole[h];

				for (int i = 0; i < mesh.Length; i++)
				{
					Vector2f v = mesh[i] - holePoint;

					if (Vector2f.LengthSquared(in v) >= dist)
						continue;

					// Do not create any straight lines when inserting the hole
					//if (MathF.Abs(MathHelpers.Angle(in holePoint, in mesh[i], in mesh[i + 1]) - MathF.PI) < 0.1f)
					//    continue;

					if (HasBridge(i, mesh, margin))
						continue;

					if (IntersectAny(in mesh[i], in holePoint, mesh, margin))
						continue;

					if (IntersectAny(in mesh[i], in holePoint, hole, margin))
						continue;

					closest = i;
					dist = Vector2f.LengthSquared(in v);
				}

				// Searchinbg all possibilities reduces chance of edge case causing triaangulation to fail.
				// If we found a valid location to insert the hole, stop looking
				//if (dist != float.MaxValue)
				//	break;
			}

			if (closest == -1)
				throw new Exception("Unable to find valid location to insert hole.");

			// Stitch together mesh that includes hole
			int totalVerts = mesh.Length + hole.Length + 2;
			if (dest.Length < totalVerts)
				throw new ArgumentException($"Destination span is too small, expected {totalVerts} but got {dest.Length}.");

			SpanList<Vector2f> vertBuilder = new(dest);

			vertBuilder.Add(mesh.Slice(0, closest + 1));
			vertBuilder.Add(hole);
			vertBuilder.Add(hole[0]);
			vertBuilder.Add(mesh[closest]);
			vertBuilder.Add(mesh.Slice(closest + 1));
		}

		public static Memory<int> Triangulate(ReadOnlySpan<Vector2f> verticies, bool clockwise = true, float margin = 0.001f)
		{
			using var vertMapBuff = MemoryPool<int>.Shared.Rent(verticies.Length);
			SpanList<int> vertMap = vertMapBuff.Memory.Span.Slice(0, verticies.Length);
			//List<int> vertMap = new();
			for (int i = 0; i < verticies.Length; i++)
			{
				vertMap.Add(i);
			}

			/*
			{
				Console.WriteLine($"Whole:");
				var vms = vertMap.AsSpan();
				for (int i = 0; i < vertMap.Count; i++)
				{
					var point = verticies[vms[i]];
					//Console.WriteLine($"{point.x},{point.y}"); // Excel
					Console.WriteLine($"p.Add(new ({point.x}f,{point.y}f));"); // Debug World
				}
			}
			*/

			List<int> indicies = new List<int>();
			while (vertMap.Count > 3)
			{
				//var vms = vertMap.AsSpan();
				if (!TryFindEar(verticies, vertMap.AsSpan(), clockwise, margin, false, out Ear ear))
				//if (!TryFindEar(verticies, CollectionsMarshal.AsSpan(vertMap), clockwise, margin, out Ear ear))
				{
					// Uncomment to debug.
					/*
					Console.WriteLine($"Remaining:");
					for (int i = 0; i < vertMap.Count; i++)
					{
						var point = verticies[vms[i]];
						//Console.WriteLine($"{point.x},{point.y}"); // Excel
						Console.WriteLine($"p.Add(new ({point.x}f,{point.y}f));"); // Debug World
					}
					*/
					Console.WriteLine($"Whole:");
					for (int i = 0; i < verticies.Length; i++)
					{
						var point = verticies[i];
						//Console.WriteLine($"{point.x},{point.y}"); // Excel
						Console.WriteLine($"p.Add(new ({point.x}f,{point.y}f));"); // Debug World
					}

					throw new Exception("Unable to complete mesh triangualtion.");
				}

				indicies.Add(ear.prev);
				indicies.Add(ear.curr);
				indicies.Add(ear.next);

				vertMap.RemoveElement(in ear.curr);
				//vertMap.Remove(ear.curr);
			}

			// Remaining verticies should be excatly one ear
			if (vertMap.Count != 3)
				throw new Exception();

			indicies.Add(vertMap[0]);
			indicies.Add(vertMap[1]);
			indicies.Add(vertMap[2]);

			return indicies.ToArray();
		}

		public static Memory<int> TriangulateStep(ReadOnlySpan<Vector2f> verticies, int steps, bool clockwise = true, float margin = 0.001f)
		{
			//using var vertMapBuff = MemoryPool<int>.Shared.Rent(verticies.Length);
			//SpanList<int> vertMap = vertMapBuff.Memory.Span.Slice(0, verticies.Length);
			List<int> vertMap = new();
			for (int i = 0; i < verticies.Length; i++)
			{
				vertMap.Add(i);
			}

			List<int> indicies = new List<int>();
			int step = 0;
			while (step < steps)
			{
				if (!TryFindEar(verticies, CollectionsMarshal.AsSpan(vertMap), clockwise, margin, false, out Ear ear))
				{
					Console.WriteLine("Unable to complete mesh triangualtion.");
					TryFindEar(verticies, CollectionsMarshal.AsSpan(vertMap), clockwise, margin, true, out ear);

					return indicies.ToArray();
					//throw new Exception("Unable to complete mesh triangualtion.");
				}

				indicies.Add(ear.prev);
				indicies.Add(ear.curr);
				indicies.Add(ear.next);

				vertMap.Remove(ear.curr);
				step++;
			}

			// Remaining verticies should be excatly one ear
			if (vertMap.Count == 3)
			{
				indicies.Add(vertMap[0]);
				indicies.Add(vertMap[1]);
				indicies.Add(vertMap[2]);
			}

			return indicies.ToArray();
		}

		static bool TryFindEar(scoped ReadOnlySpanRingBuffer<Vector2f> verticies, scoped ReadOnlySpanRingBuffer<int> vertMap, bool clockwise, float margin, bool doPrint, out Ear ear)
		{
			for (int i = 0; i < vertMap.Length; i++)
			{
				Vector2f prev2 = verticies[vertMap[i - 2]];
				Vector2f prev = verticies[vertMap[i - 1]];
				Vector2f curr = verticies[vertMap[i]];
				Vector2f next = verticies[vertMap[i + 1]];

				var aLine = clockwise ? VectorMath.Angle(next, prev, curr) : VectorMath.Angle(curr, prev, next);
				var aPrev = clockwise ? VectorMath.Angle(prev2, prev, curr) : VectorMath.Angle(curr, prev, prev2);
				var aCurr = clockwise ? VectorMath.Angle(prev, curr, next) : VectorMath.Angle(next, curr, prev);

				if (aCurr >= MathF.PI || aPrev < aLine || MathF.Abs(aCurr - MathF.PI) < margin)
					continue;

				if (!IntersectOrContainsAny(in prev, in curr, in next, verticies, margin, doPrint))
				{
					ear = new Ear()
					{
						prev = vertMap[i - 1],
						curr = vertMap[i],
						next = vertMap[i + 1]
					};

					return true;
				}
			}

			ear = default;
			return false;
		}

		static bool IntersectOrContainsAny(ref readonly Vector2f p1, ref readonly Vector2f p2, ref readonly Vector2f p3, scoped ReadOnlySpanRingBuffer<Vector2f> verticies, float margin, bool doPrint)
		{
			if (doPrint)
				Console.WriteLine($"P1: {p1}, P2: {p2}, P3: {p3}");

			for (int i = 0; i < verticies.Length; i++)
			{
				if (MathHelpers.IsClose(in p1, in verticies[i], margin) || MathHelpers.IsClose(in p1, in verticies[i + 1], margin))
					continue;

				if (MathHelpers.IsClose(in p3, in verticies[i], margin) || MathHelpers.IsClose(in p3, in verticies[i + 1], margin))
					continue;

				if (MathHelpers.Intersect(in p1, in p3, in verticies[i], in verticies[i + 1]))
				{
					if (doPrint)
						Console.WriteLine($"\t{i}: Intersect {verticies[i]} & {verticies[i + 1]}");

					return true;
				}

				if (MathHelpers.IsClose(in p2, in verticies[i], margin))
					continue;

				if (MathHelpers.IsInsideTriangle(in p1, in p2, in p3, in verticies[i]))
				{
					if (doPrint)
						Console.WriteLine($"\t{i}: IsInsideTriangle {verticies[i]}");

					return true;
				}
			}

			return false;
		}

		static bool IntersectAny(ref readonly Vector2f p1, ref readonly Vector2f p2, scoped ReadOnlySpanRingBuffer<Vector2f> verticies, float margin)
		{
			for (int i = 0; i < verticies.Length; i++)
			{
				if (MathHelpers.IsClose(in p1, in verticies[i], margin) || MathHelpers.IsClose(in p1, in verticies[i + 1], margin))
					continue;

				if (MathHelpers.IsClose(in p2, in verticies[i], margin) || MathHelpers.IsClose(in p2, in verticies[i + 1], margin))
					continue;

				if (MathHelpers.Intersect(in p1, in p2, in verticies[i], in verticies[i + 1]))
					return true;
			}

			return false;
		}

		static bool HasBridge(int skip, scoped ReadOnlySpanRingBuffer<Vector2f> verticies, float margin)
		{
			for (int i = 0; i < verticies.Length; i++)
			{
				if (i == skip)
					continue;

				if (MathHelpers.IsClose(in verticies[skip], in verticies[i], margin))
					return true;
			}

			return false;
		}
	}
}
