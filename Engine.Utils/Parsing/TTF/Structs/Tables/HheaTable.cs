using UtilLib.Stream;

namespace Engine.Utils.Parsing.TTF
{
	struct HheaTable
	{
		public float version;
		public short ascent;
		public short descent;
		public short lineGap;
		public ushort advanceWidthMax;
		public short minLeftSideBearing;
		public short minRightSideBearing;
		public short xMaxExtent;
		public short caretSlopeRise;
		public short caretSlopeRun;
		public short caretOffset;
		public short metricDataFormat;
		public ushort numOfLongHorMetrics;

		public static HheaTable ReadHheaData(DataReader reader)
		{
			HheaTable table = new HheaTable()
			{
				version = reader.ReadFloat(),
				ascent = reader.ReadInt16(),
				descent = reader.ReadInt16(),
				lineGap = reader.ReadInt16(),
				advanceWidthMax = reader.ReadUInt16(),
				minLeftSideBearing = reader.ReadInt16(),
				minRightSideBearing = reader.ReadInt16(),
				xMaxExtent = reader.ReadInt16(),
				caretSlopeRise = reader.ReadInt16(),
				caretSlopeRun = reader.ReadInt16(),
				caretOffset = reader.ReadInt16(),
			};

			// Four reserved numbers
			reader.ReadInt16();
			reader.ReadInt16();
			reader.ReadInt16();
			reader.ReadInt16();

			table.metricDataFormat = reader.ReadInt16();
			table.numOfLongHorMetrics = reader.ReadUInt16();

			return table;
		}
	}
}
