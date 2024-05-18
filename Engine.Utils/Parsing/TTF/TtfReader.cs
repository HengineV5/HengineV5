using EnCS;
using System.Buffers.Binary;

namespace Engine.Utils.Parsing.TTF
{
	internal class TtfReader : IDisposable
    {
		public long Position => reader.BaseStream.Position;

        BinaryReader reader;

        public TtfReader(Stream stream)
        {
            reader = new BinaryReader(stream);
        }

		public void Dispose()
		{
            reader.Dispose();
		}

		public ulong ReadUInt64()
			=> BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(reader.ReadUInt64()) : reader.ReadUInt64();

		public long ReadInt64()
			=> BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(reader.ReadInt64()) : reader.ReadInt64();

		public uint ReadUInt32()
            => BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(reader.ReadUInt32()) : reader.ReadUInt32();

		public int ReadInt32()
			=> BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(reader.ReadInt32()) : reader.ReadInt32();

		public ushort ReadUInt16()
			=> BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(reader.ReadUInt16()) : reader.ReadUInt16();

		public short ReadInt16()
			=> BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(reader.ReadInt16()) : reader.ReadInt16();

		public float ReadFloat()
			=> reader.ReadSingle();

		public byte ReadByte()
			=> reader.ReadByte();

		public void Read(Span<byte> buffer)
            => reader.Read(buffer);

		public void Read(Span<char> buffer)
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
