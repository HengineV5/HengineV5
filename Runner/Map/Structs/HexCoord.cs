using System.Runtime.CompilerServices;

namespace Runner
{
	struct HexCoord
	{
		public int X;
		public int Y;

		public HexCoord(int value)
		{
			X = value;
			Y = value;
		}

		public HexCoord(int x, int y)
		{
			X = x;
			Y = y;
		}

		public override string ToString()
		{
			return $"<{X.ToString()}, {Y.ToString()}>";
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static HexCoord operator +(HexCoord left, HexCoord right)
		{
			return new HexCoord(
				left.X + right.X,
				left.Y + right.Y
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static HexCoord operator -(HexCoord left, HexCoord right)
		{
			return new HexCoord(
				left.X - right.X,
				left.Y - right.Y
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static HexCoord operator *(HexCoord left, HexCoord right)
		{
			return new HexCoord(
				left.X * right.Y,
				left.Y * right.X
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static HexCoord operator *(HexCoord left, int right)
		{
			return left * new HexCoord(right);
		}
	}
}