using System.Numerics;

namespace Engine.Utils
{
	public static class Bezier
	{
		public static Vector3 QuadraticBezierCurve(Vector3 p1, Vector3 p2, Vector3 p3, float t)
		{
			return MathF.Pow(1 - t, 2) * p1 + 2 * (1 - t) * t * p2 + MathF.Pow(t, 2) * p3;
		}

		public static Vector3 CubicBezierCurve(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, float t)
		{
			return MathF.Pow(1 - t, 3) * p1 + 3 * MathF.Pow(1 - t, 2) * t * p2 + 3 * (1 - t) * MathF.Pow(t, 2) * p3 + MathF.Pow(t, 3) * p4;
		}
	}
}
