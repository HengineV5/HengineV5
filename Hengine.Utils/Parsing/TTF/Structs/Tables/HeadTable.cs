using UtilLib.Stream;

namespace Hengine.Utils.Parsing.TTF
{
	struct HeadTable
	{
		public float version;
		public float fontRevision;
		public uint checksumAdjustment;
		public uint magicNumber;
		public ushort flags;
		public ushort unitsPerEm;
		public long created;
		public long modified;
		public short xMin;
		public short yMin;
		public short xMax;
		public short yMax;
		public ushort macStyle;
		public ushort lowestRecPPEM;
		public short fontDirectionHint;
		public short indexToLocFormat;
		public short glyphDatasFormat;

		public static HeadTable ReadHeadData(DataReader reader)
		{
			return new HeadTable()
			{
				version = reader.ReadFloat(),
				fontRevision = reader.ReadFloat(),
				checksumAdjustment = reader.ReadUInt32(),
				magicNumber = reader.ReadUInt32(),
				flags = reader.ReadUInt16(),
				unitsPerEm = reader.ReadUInt16(),
				created = reader.ReadInt64(),
				modified = reader.ReadInt64(),
				xMin = reader.ReadInt16(),
				yMin = reader.ReadInt16(),
				xMax = reader.ReadInt16(),
				yMax = reader.ReadInt16(),
				macStyle = reader.ReadUInt16(),
				lowestRecPPEM = reader.ReadUInt16(),
				fontDirectionHint = reader.ReadInt16(),
				indexToLocFormat = reader.ReadInt16(),
				glyphDatasFormat = reader.ReadInt16(),
			};
		}
	}
}
