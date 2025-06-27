using UtilLib.Stream;

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

		public static MaxpTable ReadMaxpData(DataReader reader)
		{
			return new MaxpTable()
			{
				version = reader.ReadUInt32(),
				numGlyphs = reader.ReadUInt16(),
				maxPoints = reader.ReadUInt16(),
				maxContours = reader.ReadUInt16(),
				maxComponentPoints = reader.ReadUInt16(),
				maxComponentContours = reader.ReadUInt16(),
				maxZones = reader.ReadUInt16(),
				maxTwilightPoints = reader.ReadUInt16(),
				maxStorage = reader.ReadUInt16(),
				maxFunctionDefs = reader.ReadUInt16(),
				maxInstructionDefs = reader.ReadUInt16(),
				maxStackElements = reader.ReadUInt16(),
				maxSizeOfInstructions = reader.ReadUInt16(),
				maxComponentElements = reader.ReadUInt16(),
				maxComponentDepth = reader.ReadUInt16()
			};
		}
	}
}
