using UtilLib.Stream;

namespace Engine.Utils.Parsing.TTF
{
	public struct HmtxTable
	{
		public LongHorMetric[] hMetrics;
		public short[] leftSideBearing;

		internal static HmtxTable ReadHmtxData(DataReader reader, in HheaTable hheaTable)
		{
			var table = new HmtxTable
			{
				hMetrics = new LongHorMetric[hheaTable.numOfLongHorMetrics],
			};

			for (int i = 0; i < table.hMetrics.Length; i++)
			{
				table.hMetrics[i].advanceWidth = reader.ReadUInt16();
				reader.ReadUInt16(); // Ignore
			}

			return table;
		}
	}
}
