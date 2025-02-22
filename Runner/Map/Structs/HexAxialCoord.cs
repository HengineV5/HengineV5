using System.Runtime.CompilerServices;

namespace Runner
{
	struct HexAxialCoord
	{
		public int Q;
		public int R;

		public HexAxialCoord(int value)
		{
			Q = value;
			R = value;
		}

		public HexAxialCoord(int q, int r)
		{
			Q = q;
			R = r;
		}

		public override string ToString()
		{
			return $"<{Q.ToString()}, {R.ToString()}>";
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static HexAxialCoord operator +(HexAxialCoord left, HexAxialCoord right)
		{
			return new HexAxialCoord(
				left.Q + right.Q,
				left.R + right.R
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static HexAxialCoord operator -(HexAxialCoord left, HexAxialCoord right)
		{
			return new HexAxialCoord(
				left.Q - right.Q,
				left.R - right.R
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static HexAxialCoord operator *(HexAxialCoord left, HexAxialCoord right)
		{
			return new HexAxialCoord(
				left.Q * right.Q,
				left.R * right.R
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static HexAxialCoord operator *(HexAxialCoord left, int right)
		{
			return left * new HexAxialCoord(right);
		}
	}
}