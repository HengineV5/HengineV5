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
	}
}
