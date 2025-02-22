using EnCS;
using System.Buffers.Binary;

namespace Engine.Utils.Parsing.TTF
{
	internal class TtfReader : IDisposable
    {
		public long Position => reader.BaseStream.Position;

		BinaryReader reader;
		bool invertEndianess;

		public TtfReader(Stream stream, bool isLittleEndian = false)
		{
			reader = new BinaryReader(stream);
			this.invertEndianess = isLittleEndian != BitConverter.IsLittleEndian; // If system and data endianess is mismatched invert endianess
		}

		public void Dispose()
		{
            reader.Dispose();
		}

		public float ReadF2Dot14()
			=> ((float)ReadInt16()) / (1 << 14);

		public ulong ReadUInt64()
			=> invertEndianess ? BinaryPrimitives.ReverseEndianness(reader.ReadUInt64()) : reader.ReadUInt64();

		public long ReadInt64()
			=> invertEndianess ? BinaryPrimitives.ReverseEndianness(reader.ReadInt64()) : reader.ReadInt64();

		public uint ReadUInt32()
			=> invertEndianess ? BinaryPrimitives.ReverseEndianness(reader.ReadUInt32()) : reader.ReadUInt32();

		public int ReadInt32()
			=> invertEndianess ? BinaryPrimitives.ReverseEndianness(reader.ReadInt32()) : reader.ReadInt32();

		public ushort ReadUInt16()
			=> invertEndianess ? BinaryPrimitives.ReverseEndianness(reader.ReadUInt16()) : reader.ReadUInt16();

		public short ReadInt16()
			=> invertEndianess ? BinaryPrimitives.ReverseEndianness(reader.ReadInt16()) : reader.ReadInt16();

		public float ReadFloat()
			=> reader.ReadSingle();

		public byte ReadByte()
			=> reader.ReadByte();

		public char ReadChar()
			=> reader.ReadChar();

		public sbyte ReadSByte()
			=> reader.ReadSByte();

		public int Read(Span<byte> buffer)
			=> reader.Read(buffer);

		public FixedArray2<byte> ReadArray2()
        {
            FixedArray2<byte> arr = new();
            Read(arr);

            return arr;
        }

		public FixedArray4<byte> ReadArray4()
		{
			FixedArray4<byte> arr = new();
			Read(arr);

			return arr;
		}

		public void Seek(long position)
		{
			reader.BaseStream.Position = position;
		}
	}
}
