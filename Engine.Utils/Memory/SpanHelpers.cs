using System.Numerics;

namespace Engine.Utils
{
	public static class SpanHelpers
	{
		public static void Round(this Span<Vector2> span, int digis = 0)
		{
			for (int i = 0; i < span.Length; i++)
			{
				span[i].X = MathF.Round(span[i].X, digis);
				span[i].Y = MathF.Round(span[i].Y, digis);
			}
		}
	}
}
