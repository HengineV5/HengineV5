using UtilLib.Stream;

namespace Hengine.Utils.Parsing.TTF
{
	struct CmapTable
	{
		public ushort format;
		public ushort language;

		public ushort[] endCode;
		public ushort[] startCode;
		public ushort[] idDelta;
		public ushort[] idRangeOffset;
		public ushort[] glyphIndexArray;

		public static CmapTable ReadCmapData(DataReader reader)
		{
			long cmapBase = reader.Position;

			ushort version = reader.ReadUInt16();
			ushort subtableCount = reader.ReadUInt16();

			scoped Span<CmapSubtable> subtables = stackalloc CmapSubtable[subtableCount];

			for (int i = 0; i < subtableCount; i++)
			{
				subtables[i] = new()
				{
					platformID = reader.ReadUInt16(),
					platformSpecificID = reader.ReadUInt16(),
					offset = reader.ReadInt32()
				};
			}

			reader.Seek(cmapBase + subtables[0].offset);
			return ReadCmapFormat4(reader);
		}

		static CmapTable ReadCmapFormat4(DataReader reader)
		{
			ushort format = reader.ReadUInt16();
			if (format != 4)
				throw new Exception($"Parsing wrong format, expected 4 got {format}");

			ushort length = reader.ReadUInt16();
			ushort language = reader.ReadUInt16();
			ushort segCountX2 = reader.ReadUInt16();
			int segCount = segCountX2 / 2;
			ushort searchRange = reader.ReadUInt16();
			ushort entrySelector = reader.ReadUInt16();
			ushort rangeShift = reader.ReadUInt16();

			CmapTable cmap = new CmapTable();
			cmap.format = format;
			cmap.language = language;

			cmap.endCode = new ushort[segCount];
			for (int i = 0; i < cmap.endCode.Length; i++)
			{
				cmap.endCode[i] = reader.ReadUInt16();
			}

			ushort reservePad = reader.ReadUInt16();

			cmap.startCode = new ushort[segCount];
			for (int i = 0; i < cmap.startCode.Length; i++)
			{
				cmap.startCode[i] = reader.ReadUInt16();
			}

			cmap.idDelta = new ushort[segCount];
			for (int i = 0; i < cmap.idDelta.Length; i++)
			{
				cmap.idDelta[i] = reader.ReadUInt16();
			}

			cmap.idRangeOffset = new ushort[segCount];
			for (int i = 0; i < cmap.idRangeOffset.Length; i++)
			{
				cmap.idRangeOffset[i] = reader.ReadUInt16();
			}

			cmap.glyphIndexArray = new ushort[segCount];
			for (int i = 0; i < cmap.glyphIndexArray.Length; i++)
			{
				cmap.glyphIndexArray[i] = reader.ReadUInt16();
			}

			return cmap;
		}
	}

	struct CmapSubtable
	{
		public ushort platformID;
		public ushort platformSpecificID;
		public int offset;
	}
}
