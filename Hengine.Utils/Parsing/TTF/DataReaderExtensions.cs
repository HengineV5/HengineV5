using UtilLib.Memory;
using UtilLib.Stream;

namespace Hengine.Utils.Parsing.TTF
{
	static class DataReaderExtensions
	{
		public static float ReadF2Dot14(this DataReader reader)
			=> ((float)reader.ReadInt16()) / (1 << 14);

		public static FixedBuffer2<byte> ReadArray2(this DataReader reader)
		{
			FixedBuffer2<byte> arr = new();
			reader.Read(arr);

			return arr;
		}

		public static FixedBuffer4<byte> ReadArray4(this DataReader reader)
		{
			FixedBuffer4<byte> arr = new();
			reader.Read(arr);

			return arr;
		}
	}
}
