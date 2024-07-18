namespace Engine.Utils
{
	public ref struct SpanList<T>
	{
		public ref T this[int idx] => ref span[idx];

		public readonly int Count => idx;

		Span<T> span;
		int idx = 0;

		public SpanList(Span<T> span)
		{
			this.span = span;
		}

		public void Add(scoped ReadOnlySpan<T> span)
		{
			span.CopyTo(this.span.Slice(idx));
			idx += span.Length;
		}

		public void Add(in T item)
		{
			this.span[idx] = item;
			idx++;
		}

		public void Remove(int idx)
		{
			span.Slice(idx + 1).TryCopyTo(span.Slice(idx));
			this.idx--;
		}

		public void Swap(int i1, int i2)
		{
			if (i1 >= Count || i2 >= Count)
				throw new IndexOutOfRangeException();

			(span[i2], span[i1]) = (span[i1], span[i2]);
		}

		public void Cycle(int i1, int i2)
		{
			if (i1 >= Count || i2 >= Count)
				throw new IndexOutOfRangeException();

			T item = span[i2];
			span.Slice(i1, i2 - i1).CopyTo(span.Slice(i1 + 1));
			span[i1] = item;
		}

		public void Sort(Comparison<T> comparison)
		{
			span.Slice(0, Count).Sort(comparison);
		}

		public void Clear()
		{
			idx = 0;
			span.Clear();
		}

		public void Reverse()
			=> span.Slice(0, Count).Reverse();

		public ReadOnlySpan<T> AsSpan()
			=> span.Slice(0, Count);

		public static implicit operator SpanList<T>(Span<T> span)
			=> new(span);
	}
}
