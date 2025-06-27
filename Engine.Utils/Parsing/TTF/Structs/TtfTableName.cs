using UtilLib.Memory;

namespace Engine.Utils.Parsing.TTF
{
	struct TtfTableName : IEquatable<TtfTableName>
	{
		public FixedBuffer4<char> name;

		public TtfTableName(FixedBuffer4<char> name)
		{
			this.name = name;
		}

		public bool Equals(TtfTableName other)
		{
			return ((Span<char>)name).SequenceEqual(other.name);
		}

		public static implicit operator TtfTableName(scoped ReadOnlySpan<char> str)
		{
			if (str.Length != 4)
				throw new ArgumentException("Invalid table name");

			var name = new FixedBuffer4<char>();
			str.TryCopyTo(name);

			return new(name);
		}
	}
}
