using EnCS;
using System.Text;

namespace Engine.Parsing
{
	struct TtfTableDirectory
    {
        public FixedArray4<byte> tag;
        public uint checksum;
        public uint offset;
        public uint length;

		public string TagAsString()
			=> Encoding.ASCII.GetString(tag);

	}
}
