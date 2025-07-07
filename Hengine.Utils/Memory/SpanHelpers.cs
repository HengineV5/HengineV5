
namespace Hengine.Utils
{
	public static class SpanHelpers
	{
		public static void Round(this Span<Vector2f> span, int digis = 0)
		{
			for (int i = 0; i < span.Length; i++)
			{
				span[i].x = MathF.Round(span[i].x, digis);
				span[i].y = MathF.Round(span[i].y, digis);
			}
		}
	}
}
