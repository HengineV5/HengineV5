using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UtilLib.Span;
using UtilLib.Extensions;

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

			Span<int> skips = stackalloc int[1];
			for (int i = 0; i < mesh.Length; i++)
			{
				Vector2f v = mesh[i] - hole[0];

				if (Vector2f.LengthSquared(in v) > dist)
					continue;

				skips[0] = i;
				if (IntersectAny(mesh[i], hole[0], skips, mesh, margin))
					continue;

				skips[0] = 0;
				if (IntersectAny(mesh[i], hole[0], skips, hole, margin))
					continue;

				closest = i;
				dist = Vector2f.LengthSquared(in v);
			}

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
					Console.WriteLine($"{point.x},{point.y}");
				}
			}
			*/

			List<int> indicies = new List<int>();
			while (vertMap.Count > 3)
			{
				var vms = vertMap.AsSpan();
				if (!TryFindEar(verticies, vertMap.AsSpan(), clockwise, margin, out Ear ear))
				{
					// Uncomment to debug.
					/*
					Console.WriteLine($"Remaining:");
					for (int i = 0; i < vertMap.Count; i++)
					{
						var point = verticies[vms[i]];
						Console.WriteLine($"{point.x},{point.y}");
					}
					*/

					throw new Exception("Unable to complete mesh triangualtion.");
				}

				indicies.Add(ear.prev);
				indicies.Add(ear.curr);
				indicies.Add(ear.next);

				vertMap.RemoveElement(in ear.curr);
			}

			// Remaining verticies should be excatly one ear
			if (vertMap.Count != 3)
				throw new Exception();

			indicies.Add(vertMap[0]);
			indicies.Add(vertMap[1]);
			indicies.Add(vertMap[2]);

			return indicies.ToArray();
		}

		static bool TryFindEar(scoped ReadOnlySpanRingBuffer<Vector2f> verticies, scoped ReadOnlySpanRingBuffer<int> vertMap, bool clockwise, float margin, out Ear ear)
		{
			Span<int> excludeIndicies = stackalloc int[3];

			for (int i = 1; i < vertMap.Length + 2; i++)
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

				excludeIndicies[0] = vertMap[i - 1];
				excludeIndicies[1] = vertMap[i];
				excludeIndicies[2] = vertMap[i + 1];
				if (!IntersectAny(prev, next, excludeIndicies, verticies, margin))
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static int Loop(int idx, int modulo)
		{
			return (idx + modulo) % modulo;
		}

		static bool IntersectAny(Vector2f p1, Vector2f p2, scoped ReadOnlySpan<int> skips, scoped ReadOnlySpanRingBuffer<Vector2f> verticies, float margin)
		{
			for (int i = 0; i < verticies.Length - 1; i++)
			{
				if (VectorMath.IsClose(p1, verticies[i], margin) || VectorMath.IsClose(p1, verticies[i + 1], margin))
					continue;

				if (VectorMath.IsClose(p2, verticies[i], margin) || VectorMath.IsClose(p2, verticies[i + 1], margin))
					continue;

				if (skips.Contains(i))
					continue;

				if (skips.Contains(i + 1))
					continue;

				if (VectorMath.Intersect(p1, p2, verticies[i], verticies[i + 1], margin))
					return true;
			}

			if (!FoundSkip(verticies.Length - 1, skips) && !FoundSkip(0, skips) && VectorMath.Intersect(p1, p2, verticies[verticies.Length - 1], verticies[0], margin))
				return true;

			return false;
		}

		static bool FoundSkip(int i, ReadOnlySpan<int> skips)
		{
			for (int a = 0; a < skips.Length; a++)
			{
				if (i == skips[a])
					return true;
			}

			return false;
		}
	}
}
