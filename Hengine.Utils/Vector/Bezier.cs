
namespace Hengine.Utils
{
	public static class Bezier
	{
		public static Vector3f QuadraticBezierCurve(Vector3f p1, Vector3f p2, Vector3f p3, float t)
		{
			return MathF.Pow(1 - t, 2) * p1 + 2 * (1 - t) * t * p2 + MathF.Pow(t, 2) * p3;
		}

		public static Vector3f CubicBezierCurve(Vector3f p1, Vector3f p2, Vector3f p3, Vector3f p4, float t)
		{
			return MathF.Pow(1 - t, 3) * p1 + 3 * MathF.Pow(1 - t, 2) * t * p2 + 3 * (1 - t) * MathF.Pow(t, 2) * p3 + MathF.Pow(t, 3) * p4;
		}
	}
}
