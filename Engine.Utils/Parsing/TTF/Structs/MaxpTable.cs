namespace Engine.Utils.Parsing.TTF
{
	struct MaxpTable
	{
		public uint version;
		public ushort numGlyphs;
		public ushort maxPoints;
		public ushort maxContours;
		public ushort maxComponentPoints;
		public ushort maxComponentContours;
		public ushort maxZones;
		public ushort maxTwilightPoints;
		public ushort maxStorage;
		public ushort maxFunctionDefs;
		public ushort maxInstructionDefs;
		public ushort maxStackElements;
		public ushort maxSizeOfInstructions;
		public ushort maxComponentElements;
		public ushort maxComponentDepth;
	}
}
