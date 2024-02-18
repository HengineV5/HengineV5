using System.Numerics;
using System.Runtime.CompilerServices;

namespace Runner
{
	struct HexCubeCoord
	{
		public int Q;
		public int R;
		public int S;

		public HexCubeCoord(int value)
		{
			Q = value;
			R = value;
			S = value;
		}

		public HexCubeCoord(int q, int r, int s)
		{
			Q = q;
			R = r;
			S = s;
		}

		public override string ToString()
		{
			return $"<{Q.ToString()}, {R.ToString()}, {S.ToString()}>";
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static HexCubeCoord operator +(HexCubeCoord left, HexCubeCoord right)
		{
			return new HexCubeCoord(
				left.Q + right.Q,
				left.R + right.R,
				left.S + right.S
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static HexCubeCoord operator -(HexCubeCoord left, HexCubeCoord right)
		{
			return new HexCubeCoord(
				left.Q - right.Q,
				left.R - right.R,
				left.S - right.S
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static HexCubeCoord operator *(HexCubeCoord left, HexCubeCoord right)
		{
			return new HexCubeCoord(
				left.Q * right.Q,
				left.R * right.R,
				left.S * right.S
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static HexCubeCoord operator *(HexCubeCoord left, int right)
		{
			return left * new HexCubeCoord(right);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Distance(HexCubeCoord value1, HexCubeCoord value2)
		{
			var ab = value1 - value2;
			return Math.Max(Math.Abs(ab.Q), Math.Max(Math.Abs(ab.R), Math.Abs(ab.S)));
		}
	}
}