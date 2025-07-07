using EnCS;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UtilLib.Span;
using UtilLib.Stream;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Hengine.Utils.Parsing.TTF
{
	public class TtfLoader
	{
		public static Font LoadFont(string path)
		{
			using DataReader reader = new DataReader(new FileStream(path, FileMode.Open, FileAccess.Read));
            return Font.Load(reader);
        }
	}

    public class Font
    {
		GlyphData[] glyphData;

		HmtxTable hmtx;
		CmapTable cmapData;

		// https://github.com/LayoutFarm/Typography/blob/master/Typography.OpenFont/Tables/CharacterMap.cs
		public GlyphData GetGlyphIndex(ushort unicode)
		{
			return glyphData[GetGlyphIdx(ref cmapData, unicode)];
		}

		public ushort GetGlyphAdvance(ushort unicode)
		{
			return hmtx.hMetrics[GetGlyphIdx(ref cmapData, unicode)].advanceWidth;
		}

		static int GetGlyphIdx(ref readonly CmapTable cmapData, ushort unicode)
		{
			int idx = -1;
			for (int i = 0; i < cmapData.endCode.Length; i++)
			{
				if (cmapData.endCode[i] >= unicode)
				{
					idx = i;
					break;
				}
			}

			if (cmapData.idRangeOffset[idx] == 0)
			{
				return (unicode + cmapData.idDelta[idx]) % 65536;
			}
			else
			{
				int offset = cmapData.idRangeOffset[idx] / 2 + (unicode - cmapData.startCode[idx]);
				return cmapData.glyphIndexArray[offset - cmapData.idRangeOffset.Length + idx];
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public GlyphData GetGlyphIndex(uint unicode)
			=> GetGlyphIndex((ushort)unicode);


		internal static Font Load(DataReader reader)
        {
			TtfOffsetSubtable offsetSubtable = new()
			{
				scalerType = reader.ReadUInt32(),
				numTables = reader.ReadUInt16(),
				searchRange = reader.ReadUInt16(),
				entrySelector = reader.ReadUInt16(),
				rangeShift = reader.ReadUInt16()
			};

			if (offsetSubtable.numTables > 64)
				throw new Exception();

			SpanDictionary<TtfTableName, TtfTableDirectory> tables = new(stackalloc TtfTableName[offsetSubtable.numTables], stackalloc TtfTableDirectory[offsetSubtable.numTables]);
			for (int i = 0; i < offsetSubtable.numTables; i++)
			{
				TtfTableDirectory directory = new()
				{
					tag = reader.ReadArray4(),
					checksum = reader.ReadUInt32(),
					offset = reader.ReadUInt32(),
					length = reader.ReadUInt32()
				};

				tables.TryAdd(directory.TagAsString(), directory);
			}

			Font font = new();

			reader.Seek(tables["head"].offset);
			var headData = HeadTable.ReadHeadData(reader);

			bool isLongVersion = headData.indexToLocFormat == 1;

			reader.Seek(tables["maxp"].offset);
			var maxpData = MaxpTable.ReadMaxpData(reader);

			reader.Seek(tables["loca"].offset);

			if (offsetSubtable.numTables > 256)
				throw new Exception();

			Span<uint> locations = stackalloc uint[maxpData.numGlyphs];
			for (int i = 0; i < locations.Length; i++)
			{
				locations[i] = isLongVersion ? reader.ReadUInt32() : reader.ReadUInt16() * 2u;
			}

			reader.Seek(tables["hhea"].offset);
			var hheaTable = HheaTable.ReadHheaData(reader);

			reader.Seek(tables["hmtx"].offset);
			font.hmtx = HmtxTable.ReadHmtxData(reader, hheaTable);

            reader.Seek(tables["cmap"].offset);
			font.cmapData = CmapTable.ReadCmapData(reader);

			ref var glyphEntry = ref tables["glyf"];
			font.glyphData = new GlyphData[maxpData.numGlyphs - 1];

			var countoursBuff = MemoryPool<ushort>.Shared.Rent(maxpData.maxComponentContours * 256);
			SpanList<ushort> countoursBuffList = countoursBuff.Memory.Span;

			var verticeisBuff = MemoryPool<GlyphVertex>.Shared.Rent(maxpData.maxComponentPoints * 256);
			SpanList<GlyphVertex> verticeisBuffList = verticeisBuff.Memory.Span;

			for (int i = 0; i < font.glyphData.Length; i++)
            {
				if (locations[i + 1] - locations[i] <= 0) // Empty glyph
					continue;

                reader.Seek(glyphEntry.offset + locations[i]);

				ref GlyphData glyphData = ref font.glyphData[i];
				ReadGlyphDescription(reader, ref glyphData.description);

				GlyphOffset offset = new();
				ReadGlyphData(reader, ref glyphData.description, ref countoursBuffList, ref verticeisBuffList, in offset, glyphEntry.offset, locations);

				glyphData.contours = new ushort[countoursBuffList.Count];
				countoursBuff.Memory.Slice(0, countoursBuffList.Count).TryCopyTo(glyphData.contours);

				glyphData.coords = new GlyphVertex[verticeisBuffList.Count];
				verticeisBuff.Memory.Slice(0, verticeisBuffList.Count).TryCopyTo(glyphData.coords);

				countoursBuffList.Clear();
				verticeisBuffList.Clear();
			}

			return font;
		}

		static void ReadGlyphDescription(DataReader reader, scoped ref GlyphDescription desc)
		{
			desc = new()
			{
				numberOfContours = reader.ReadInt16(),
				xMin = reader.ReadUInt16(),
				yMin = reader.ReadUInt16(),
				xMax = reader.ReadUInt16(),
				yMax = reader.ReadUInt16()
			};
		}

		static void ReadGlyphData(DataReader reader, scoped ref GlyphDescription desc, scoped ref SpanList<ushort> contours, scoped ref SpanList<GlyphVertex> verticeis, ref readonly GlyphOffset offset, uint glyphStart, scoped Span<uint> locations) // TODO: Not the best just passing locations here, mabye fix.
		{
			if (desc.numberOfContours < 0)
			{
				ReadCompoundGlyphs(reader, ref desc, ref contours, ref verticeis, in offset, glyphStart, locations);
			}
			else
			{
				var c = contours.Reserve(desc.numberOfContours);
				ReadSimpleGlyphContours(reader, ref c, offset.contourOffset);

				var v = verticeis.Reserve(c[c.Length - 1] + 1 - offset.contourOffset); // Get the last vertex idx
				ReadSimpleGlyph(reader, ref v, in offset.vertexOffset);
			}
		}
		
		static void ReadSimpleGlyphContours(DataReader reader, scoped ref Span<ushort> contours, ushort offset)
		{
			for (int i = 0; i < contours.Length; i++)
			{
				contours[i] = (ushort)(reader.ReadUInt16() + offset);
			}

			ushort instructionLength = reader.ReadUInt16();
			reader.Seek(reader.Position + instructionLength);

			#region Unused
			/*
			byte[] instructions = new byte[instructionLength];
			for (int i = 0; i < instructions.Length; i++)
			{
				instructions[i] = reader.ReadByte();
			}
			*/
			#endregion
		}

		static void ReadSimpleGlyph(DataReader reader, scoped ref Span<GlyphVertex> verticeis, ref readonly GlyphVertex offset)
		{
			if (verticeis.Length > 1024)
				throw new Exception();

			Span<SimpleGlyphFlag> flags = stackalloc SimpleGlyphFlag[verticeis.Length];
			for (int i = 0; i < flags.Length; i++)
			{
				SimpleGlyphFlag flag = (SimpleGlyphFlag)reader.ReadByte();
				flags[i] = flag;

				if (flags[i].HasFlag(SimpleGlyphFlag.Repeat))
				{
					byte rep = reader.ReadByte();
					for (int r = 0; r < rep; r++) flags[++i] = flag;
				}
			}

			ReadCoords(reader, verticeis, flags, SimpleGlyphFlag.XByte, SimpleGlyphFlag.XSignOrSame, static (ref v, coord) =>
			{
				v.x = coord;
			});

			ReadCoords(reader, verticeis, flags, SimpleGlyphFlag.YByte, SimpleGlyphFlag.YSignOrSame, static (ref v, coord) =>
			{
				v.y = coord;
			});

			for (int i = 0; i < verticeis.Length; i++)
			{
				verticeis[i].onCurve = flags[i].HasFlag(SimpleGlyphFlag.OnCurve);

				verticeis[i].x += offset.x;
				verticeis[i].y += offset.y;
			}
		}

		static void ReadCompoundGlyphs(DataReader reader, scoped ref GlyphDescription desc, scoped ref SpanList<ushort> contours, scoped ref SpanList<GlyphVertex> verticeis, ref readonly GlyphOffset offset, uint glyphStart, scoped Span<uint> locations)
		{
			while (ReadCompoundGlyph(reader, ref desc, ref contours, ref verticeis, in offset, glyphStart, locations)) { } // Read untill there are no more components
		}

		static bool ReadCompoundGlyph(DataReader reader, scoped ref GlyphDescription desc, scoped ref SpanList<ushort> contours, scoped ref SpanList<GlyphVertex> verticeis, ref readonly GlyphOffset offset, uint glyphStart, scoped Span<uint> locations)
		{
			CompoundGlyphFlag flag = (CompoundGlyphFlag)reader.ReadUInt16();
			ushort glyphIndex = reader.ReadUInt16();

			bool isByte = flag.HasFlag(CompoundGlyphFlag.Arg1And2AreWords);
			bool isValue = flag.HasFlag(CompoundGlyphFlag.ArgsAreXYValues);

			int offsetX;
			int offsetY;

			if (isByte)
			{
				if (isValue)
				{
					offsetX = reader.ReadInt16();
					offsetY = reader.ReadInt16();
				}
				else
				{
					offsetX = reader.ReadUInt16();
					offsetY = reader.ReadUInt16();
				}
			}
			else
			{
				if (isValue)
				{
					offsetX = reader.ReadSByte();
					offsetY = reader.ReadSByte();
				}
				else
				{
					offsetX = reader.ReadByte();
					offsetY = reader.ReadByte();
				}
			}

			float scaleX;
			float scale01;
			float scale10;
			float scaleY;

			if (flag.HasFlag(CompoundGlyphFlag.WeHaveAScale))
			{
				scaleX = scaleY = reader.ReadF2Dot14();
			}
			else if (flag.HasFlag(CompoundGlyphFlag.WeHaveAnXAndYScale))
			{
				scaleX = reader.ReadF2Dot14();
				scaleY = reader.ReadF2Dot14();
			}
			else if (flag.HasFlag(CompoundGlyphFlag.WeHaveATwoByTwo))
			{
				scaleX = reader.ReadF2Dot14();
				scale01 = reader.ReadF2Dot14();
				scale10 = reader.ReadF2Dot14();
				scaleY = reader.ReadF2Dot14();
			}

			long currPosition = reader.Position;
			reader.Seek(glyphStart + locations[glyphIndex]);

			int startContour = contours.Count;
			int startVertex = verticeis.Count;

			GlyphData compountGlyph = new();
			ReadGlyphDescription(reader, ref compountGlyph.description);

			GlyphOffset newOffset = new()
			{
				contourOffset = (ushort)(startVertex + offset.contourOffset),
				vertexOffset = new()
				{
					x = offsetX + offset.vertexOffset.x,
					y = offsetY + offset.vertexOffset.y
				}
			};

			ReadGlyphData(reader, ref compountGlyph.description, ref contours, ref verticeis, in newOffset, glyphStart, locations);

			//var compoundGlyph = ReadGlyphData(reader, glyphStart, locations);
			reader.Seek(currPosition);

			/*
            for (int i = 0; i < compoundGlyph.contours.Length; i++)
			{
				compoundGlyph.contours.Span[i] += (ushort)compountGlyph.coords.Length;
			}

			for (int i = 0; i < compoundGlyph.coords.Length; i++)
			{
				compoundGlyph.coords.Span[i].x += offsetX;
				compoundGlyph.coords.Span[i].y += offsetY;
			}

			compountGlyph.contours = ArrayHelpers.Join(compountGlyph.contours, compoundGlyph.contours);
			compountGlyph.coords = ArrayHelpers.Join(compountGlyph.coords, compoundGlyph.coords);
			*/

			return flag.HasFlag(CompoundGlyphFlag.MoreComponents);
		}

		private delegate void ReadCoordsDelegate(ref GlyphVertex vertex, int coord);

		static void ReadCoords(DataReader reader, Span<GlyphVertex> vertices, Span<SimpleGlyphFlag> flags, SimpleGlyphFlag isByte, SimpleGlyphFlag signOrSame, ReadCoordsDelegate f)
		{
			int prev = 0;
			for (int i = 0; i < flags.Length; i++)
			{
				int curr = 0;

				if (flags[i].HasFlag(isByte))
				{
					curr = prev + reader.ReadByte() * (flags[i].HasFlag(signOrSame) ? 1 : -1);
				}
				else if(flags[i].HasFlag(signOrSame))
				{
					curr = prev;
				}
				else
				{
					curr = prev + reader.ReadInt16();
				}

				f(ref vertices[i], curr);
				prev = curr;
			}
		}
	}
}
