using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

		public static Memory<int> Triangulate(ReadOnlySpan<Vector2> verticies, bool clockwise = true, float margin = 0.001f)
		 => Triangulate(verticies, new List<int>(Enumerable.Range(0, verticies.Length)), clockwise, margin);

		public static Memory<Vector2> ProcessHole(ReadOnlySpan<Vector2> mesh, ReadOnlySpan<Vector2> hole, float margin = 0.001f)
		{
			int closest = -1;
			float dist = float.MaxValue;

			Span<int> skips = stackalloc int[1];
			for (int i = 0; i < mesh.Length; i++)
			{
				Vector2 v = mesh[i] - hole[0];

				if (v.LengthSquared() > dist)
					continue;

				skips[0] = i;
				if (IntersectAny(mesh[i], hole[0], skips, mesh, margin))
					continue;

				skips[0] = 0;
				if (IntersectAny(mesh[i], hole[0], skips, hole, margin))
					continue;

				closest = i;
				dist = v.LengthSquared();
			}

			// Stitch together mesh that includes hole
			int totalVerts = mesh.Length + hole.Length + 2;
			Memory<Vector2> verticies = new Vector2[totalVerts];
			SpanList<Vector2> vertBuilder = new(verticies.Span);

			vertBuilder.Add(mesh.Slice(0, closest + 1));
			vertBuilder.Add(hole);
			vertBuilder.Add(hole[0]);
			vertBuilder.Add(mesh[closest]);
			vertBuilder.Add(mesh.Slice(closest + 1));

			return verticies;
		}

		static Memory<int> Triangulate(ReadOnlySpan<Vector2> verticies, List<int> vertMap, bool clockwise, float margin)
		{
			List<int> indicies = new List<int>();
			while (vertMap.Count > 3)
			{
				if (!TryFindEar(verticies, CollectionsMarshal.AsSpan(vertMap), clockwise, margin, out Ear ear))
					throw new Exception("Unable to complete mesh triangualtion.");

				indicies.Add(ear.prev);
				indicies.Add(ear.curr);
				indicies.Add(ear.next);

				vertMap.Remove(ear.curr);
			}

			// Remaining verticies should be excatly one ear
			if (vertMap.Count != 3)
				throw new Exception();

			indicies.Add(vertMap[0]);
			indicies.Add(vertMap[1]);
			indicies.Add(vertMap[2]);

			return indicies.ToArray();
		}

		static bool TryFindEar(ReadOnlySpan<Vector2> verticies, ReadOnlySpan<int> vertMap, bool clockwise, float margin, out Ear ear)
		{
			Span<int> excludeIndicies = stackalloc int[3];

			for (int i = 1; i < vertMap.Length + 2; i++)
			{
				int prev2Idx = Loop(i - 2, vertMap.Length);
				int prevIdx = Loop(i - 1, vertMap.Length);
				int currIdx = Loop(i, vertMap.Length);
				int nextIdx = Loop(i + 1, vertMap.Length);

				Vector2 prev2 = verticies[vertMap[prev2Idx]];
				Vector2 prev = verticies[vertMap[prevIdx]];
				Vector2 curr = verticies[vertMap[currIdx]];
				Vector2 next = verticies[vertMap[nextIdx]];

				var aLine = clockwise ? VectorMath.Angle(next, prev, curr) : VectorMath.Angle(curr, prev, next);
				var aPrev = clockwise ? VectorMath.Angle(prev2, prev, curr) : VectorMath.Angle(curr, prev, prev2);
				var aCurr = clockwise ? VectorMath.Angle(prev, curr, next) : VectorMath.Angle(next, curr, prev);

				if (aCurr >= MathF.PI || aPrev < aLine || MathF.Abs(aCurr - MathF.PI) < margin)
					continue;

				excludeIndicies[0] = vertMap[prevIdx];
				excludeIndicies[1] = vertMap[currIdx];
				excludeIndicies[2] = vertMap[nextIdx];
				if (!IntersectAny(prev, next, excludeIndicies, verticies, margin))
				{
					ear = new Ear()
					{
						prev = vertMap[prevIdx],
						curr = vertMap[currIdx],
						next = vertMap[nextIdx]
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

		static bool IntersectAny(Vector2 p1, Vector2 p2, ReadOnlySpan<int> skips, ReadOnlySpan<Vector2> verticies, float margin)
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
