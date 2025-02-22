using System.Numerics;
using System.Runtime.CompilerServices;

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
		/// Check if two lines as defined by four points intersect
		/// </summary>
		/// <returns></returns> 
		public static bool Intersect(Vector2f p1, Vector2f q1, Vector2f p2, Vector2f q2, float margin)
		{
			// Find the four orientations needed for general and 
			// special cases 
			int o1 = VectorMathInternals.orientation(p1, q1, p2, margin);
			int o2 = VectorMathInternals.orientation(p1, q1, q2, margin);
			int o3 = VectorMathInternals.orientation(p2, q2, p1, margin);
			int o4 = VectorMathInternals.orientation(p2, q2, q1, margin);

			// General case 
			if (o1 != o2 && o3 != o4)
				return true;

			// Special Cases 
			// p1, q1 and p2 are collinear and p2 lies on segment p1q1 
			if (o1 == 0 && OnSegment(p1, p2, q1)) return true;

			// p1, q1 and q2 are collinear and q2 lies on segment p1q1 
			if (o2 == 0 && OnSegment(p1, q2, q1)) return true;

			// p2, q2 and p1 are collinear and p1 lies on segment p2q2 
			if (o3 == 0 && OnSegment(p2, p1, q2)) return true;

			// p2, q2 and q1 are collinear and q1 lies on segment p2q2 
			if (o4 == 0 && OnSegment(p2, q1, q2)) return true;

			return false; // Doesn't fall in any of the above cases 
		}

		/// <summary>
		/// Determine if a triangle as determined by three points is oriented clockwise or counter clockwise
		/// </summary>
		/// <returns></returns>
		public static bool IsClockwise(Vector2f p1, Vector2f p2, Vector2f p3)
		{
			Vector2f ab = p2 - p1;
			Vector2f bc = p2 - p3;

			Vector3f cross = Vector3f.Cross(Vector3f.FromVector2(ab), Vector3f.FromVector2(bc));
            return float.Sign(cross.z) == -1;
		}

		/// <summary>
		/// Determine wether a set of points is clockwise or counter clockwise
		/// </summary>
		/// <param name="points"></param>
		/// <returns></returns>
		public static bool IsClockwise(ReadOnlySpan<Vector2f> points)
		{
			float sum = 0;
            for (int i = 0; i < points.Length - 1; i++)
            {
				Vector2f p1 = points[i];
				Vector2f p2 = points[i + 1];

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

		public static bool IsClose(Vector2f p1, Vector2f p2, float margin)
		{
			return MathF.Abs(p1.x - p2.x) < margin && MathF.Abs(p1.y - p2.y) < margin;
		}

		// Given three collinear points p, q, r, the function checks if 
		// point q lies on line segment 'pr' 
		public static bool OnSegment(Vector2f p, Vector2f q, Vector2f r)
		{
			if (q.x <= Math.Max(p.x, r.x) && q.x >= Math.Min(p.x, r.x) &&
				q.y <= Math.Max(p.y, r.y) && q.y >= Math.Min(p.y, r.y))
				return true;

			return false;
		}

		// Distance from p to a line going trough a and b
		public static float DistanceToLine(Vector2f a, Vector2f b, Vector2f p)
		{
			return Vector2f.LengthSquared(((p.x - a.x) * (b.x - a.x) + (p.y - a.y) * (b.y - a.y)) / (b - a));
		}
	}

	static class VectorMathInternals
	{
		// To find orientation of ordered triplet (p, q, r). 
		// The function returns following values 
		// 0 --> p, q and r are collinear 
		// 1 --> Clockwise 
		// 2 --> Counterclockwise 
		public static int orientation(Vector2f p, Vector2f q, Vector2f r, float margin)
		{
			// See https://www.geeksforgeeks.org/orientation-3-ordered-points/ 
			// for details of below formula. 
			float val = (q.y - p.y) * (r.x - q.x) - (q.x - p.x) * (r.y - q.y);

			if (MathF.Abs(val) < margin) return 0; // collinear 

			return (val > 0) ? 1 : 2; // clock or counterclock wise 
		}
	}
}
