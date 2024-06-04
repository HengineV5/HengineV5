namespace Engine.Utils
{
	ref struct SpanList<T>
	{
		Span<T> span;
		int idx = 0;

		public SpanList(Span<T> span)
		{
			this.span = span;
		}

		public void Append(ReadOnlySpan<T> span)
		{
			span.CopyTo(this.span.Slice(idx));
			idx += span.Length;
		}

		public void Append(in T item)
		{
			this.span[idx] = item;
			idx++;
		}
	}
}
