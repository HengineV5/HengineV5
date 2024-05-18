namespace Engine.Utils.Parsing.TTF
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
	}
}
