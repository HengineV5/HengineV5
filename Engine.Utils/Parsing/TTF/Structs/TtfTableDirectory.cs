using EnCS;
using System.Text;
using UtilLib.Memory;

namespace Engine.Utils.Parsing.TTF
{
	struct TtfTableDirectory
    {
        public FixedBuffer4<byte> tag;
        public uint checksum;
        public uint offset;
        public uint length;

		public string TagAsString()
			=> Encoding.ASCII.GetString(tag);

	}
}
