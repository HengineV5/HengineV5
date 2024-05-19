namespace Engine.Utils
{
	public static class ArrayHelpers
	{
		public static T[] Join<T>(ReadOnlySpan<T> a1, ReadOnlySpan<T> a2)
		{
			T[] result = new T[a1.Length + a2.Length];

			a1.CopyTo(result.AsSpan());
			a2.CopyTo(result.AsSpan().Slice(a1.Length));

			return result;
		}


		public static T[] Join<T>(Memory<T> a1, Memory<T> a2)
		{
			T[] result = new T[a1.Length + a2.Length];

			a1.CopyTo(result);
			a2.CopyTo(result.AsMemory().Slice(a1.Length));

			return result;
		}
	}
}
