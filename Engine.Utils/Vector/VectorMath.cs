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
		public static float Angle(Vector2 a, Vector2 b, Vector2 c)
		{
			Vector2 ba = a - b;
			Vector2 bc = b - c;

			float dot = ba.X * bc.X + ba.Y * bc.Y;
			float det = ba.X * bc.Y - ba.Y * bc.X;

			return MathF.Atan2(det, dot) + MathF.PI;
		}

		/// <summary>
		/// Check if two lines as defined by four points intersect
		/// </summary>
		/// <returns></returns> 
		public static bool Intersect(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2)
		{
			// Find the four orientations needed for general and 
			// special cases 
			int o1 = VectorMathInternals.orientation(p1, q1, p2);
			int o2 = VectorMathInternals.orientation(p1, q1, q2);
			int o3 = VectorMathInternals.orientation(p2, q2, p1);
			int o4 = VectorMathInternals.orientation(p2, q2, q1);

			// General case 
			if (o1 != o2 && o3 != o4)
				return true;

			// Special Cases 
			// p1, q1 and p2 are collinear and p2 lies on segment p1q1 
			if (o1 == 0 && VectorMathInternals.onSegment(p1, p2, q1)) return true;

			// p1, q1 and q2 are collinear and q2 lies on segment p1q1 
			if (o2 == 0 && VectorMathInternals.onSegment(p1, q2, q1)) return true;

			// p2, q2 and p1 are collinear and p1 lies on segment p2q2 
			if (o3 == 0 && VectorMathInternals.onSegment(p2, p1, q2)) return true;

			// p2, q2 and q1 are collinear and q1 lies on segment p2q2 
			if (o4 == 0 && VectorMathInternals.onSegment(p2, q1, q2)) return true;

			return false; // Doesn't fall in any of the above cases 
		}

		/// <summary>
		/// Determine if a triangle as determined by three points is oriented clockwise of counter clockwise
		/// </summary>
		/// <returns></returns>
		public static bool IsClockwise(Vector2 p1, Vector2 p2, Vector2 p3)
		{
			Vector2 ab = p2 - p1;
			Vector2 bc = p2 - p3;

			Vector3 cross = Vector3.Cross(new(ab, 0), new(bc, 0));
            return float.Sign(cross.Z) == -1;
		}

		public static bool IsClockwise(Span<Vector2> points)
		{
			float sum = 0;
            for (int i = 0; i < points.Length - 1; i++)
            {
				Vector2 p1 = points[i];
				Vector2 p2 = points[i + 1];

				sum += (p2.X - p1.X) * (p2.Y + p1.Y);
            }

			return float.Sign(sum) == 1;
        }
	}

	static class VectorMathInternals
	{
		// Given three collinear points p, q, r, the function checks if 
		// point q lies on line segment 'pr' 
		public static bool onSegment(Vector2 p, Vector2 q, Vector2 r)
		{
			if (q.X <= Math.Max(p.X, r.X) && q.X >= Math.Min(p.X, r.X) &&
				q.Y <= Math.Max(p.Y, r.Y) && q.Y >= Math.Min(p.Y, r.Y))
				return true;

			return false;
		}

		// To find orientation of ordered triplet (p, q, r). 
		// The function returns following values 
		// 0 --> p, q and r are collinear 
		// 1 --> Clockwise 
		// 2 --> Counterclockwise 
		public static int orientation(Vector2 p, Vector2 q, Vector2 r)
		{
			// See https://www.geeksforgeeks.org/orientation-3-ordered-points/ 
			// for details of below formula. 
			float val = MathF.Round((q.Y - p.Y) * (r.X - q.X) - (q.X - p.X) * (r.Y - q.Y), 2);

			if (val == 0) return 0; // collinear 

			return (val > 0) ? 1 : 2; // clock or counterclock wise 
		}
	}
}
