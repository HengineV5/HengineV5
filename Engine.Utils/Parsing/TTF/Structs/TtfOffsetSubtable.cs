namespace Engine.Utils.Parsing.TTF
{
	struct TtfOffsetSubtable
    {
        public uint scalerType;
        public ushort numTables;
        public ushort searchRange;
        public ushort entrySelector;
        public ushort rangeShift;
    }
}
