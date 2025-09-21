using System.Runtime.CompilerServices;
using UtilLib.Span;

namespace Engine.Utils
{
	public static class VectorMath
	{
		/// <summary>
		/// Takes the counter clockwise internal angle between ba and bc
		/// </summary>
		/// <returns></returns>
		public static float Angle(Vector2f a, Vector2f b, Vector2f c)
		{
			Vector2f ba = a - b;
			Vector2f bc = b - c;

			float dot = ba.x * bc.x + ba.y * bc.y;
			float det = ba.x * bc.y - ba.y * bc.x;

			return MathF.Atan2(det, dot) + MathF.PI;
		}

		/// <summary>
		/// Determine wether a set of points is clockwise or counter clockwise
		/// </summary>
		/// <param name="points"></param>
		/// <returns></returns>
		public static bool IsClockwise(scoped ReadOnlySpan<Vector2f> points)
		{
			scoped ReadOnlySpanRingBuffer<Vector2f> pointsRing = points;

			float sum = 0;
			for (int i = 0; i < points.Length; i++)
			{
				Vector2f p1 = pointsRing[i];
				Vector2f p2 = pointsRing[i + 1];

				sum += (p2.x - p1.x) * (p2.y + p1.y);
			}

			return float.Sign(sum) == 1;
		}

		public static bool TryGetIntersection(Vector2f p1, Vector2f q1, Vector2f p2, Vector2f q2, float margin, out Vector2f i)
		{
			Unsafe.SkipInit(out i);
			Vector2f a = q1 - p1;
			Vector2f b = q2 - p2;

			float delta = a.y * b.x - a.x * b.y;

			if (MathF.Abs(delta) < margin)
				return false;

			float k1 = ((p1.x - p2.x) * b.y + (p2.y - p1.y) * b.x) / delta;

			i = new Vector2f(p1.x + a.x * k1, p1.y + a.y * k1);
			return true;
		}
	}
}
