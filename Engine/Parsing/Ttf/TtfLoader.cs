using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Engine.Parsing
{
	public class TtfLoader
	{
		public static Font LoadFont(string path)
		{
			using TtfReader reader = new TtfReader(new FileStream(path, FileMode.Open));
            return Font.Load(reader);
        }
	}

	struct CmapData
	{
		public ushort format;
		public ushort language;

		public ushort[] endCode;
		public ushort[] startCode;
		public ushort[] idDelta;
		public ushort[] idRangeOffset;
		public ushort[] glyphIndexArray;
	}

    public class Font
    {
		public GlyphData[] glyphData;

		CmapData cmapData;
        TtfOffsetSubtable offsetSubtable;
        List<TtfTableDirectory> tableDirectories;

		// https://github.com/LayoutFarm/Typography/blob/master/Typography.OpenFont/Tables/CharacterMap.cs
		public GlyphData GetGlyphIndex(ushort unicode)
		{
			//int idx = Array.BinarySearch(cmapData.endCode, unicode);
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
				return glyphData[(unicode + cmapData.idDelta[idx]) % 65536];
			}
			else
			{
				int offset = cmapData.idRangeOffset[idx] / 2 + (unicode - cmapData.startCode[idx]);
				return glyphData[offset - cmapData.idRangeOffset.Length + idx];
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public GlyphData GetGlyphIndex(int unicode)
			=> GetGlyphIndex((ushort)unicode);


		internal static Font Load(TtfReader reader)
        {
			Font font = new();
			font.offsetSubtable = new()
			{
				scalerType = reader.ReadUInt32(),
				numTables = reader.ReadUInt16(),
				searchRange = reader.ReadUInt16(),
				entrySelector = reader.ReadUInt16(),
				rangeShift = reader.ReadUInt16()
			};

			font.tableDirectories = new List<TtfTableDirectory>();
			for (int i = 0; i < font.offsetSubtable.numTables; i++)
			{
				font.tableDirectories.Add(new()
				{
					tag = reader.ReadArray4(),
					checksum = reader.ReadUInt32(),
					offset = reader.ReadUInt32(),
					length = reader.ReadUInt32()
				});
			}

			var glyphEntry = font.tableDirectories.Single(x => x.TagAsString() == "glyf");
			var cmapEntry = font.tableDirectories.Single(x => x.TagAsString() == "cmap");
			var maxpEntry = font.tableDirectories.Single(x => x.TagAsString() == "maxp");
			var headEntry = font.tableDirectories.Single(x => x.TagAsString() == "head");
			var locaEntry = font.tableDirectories.Single(x => x.TagAsString() == "loca");

			reader.Seek(headEntry.offset);
			var headData = ReadHeadData(reader);

			bool isLongVersion = headData.indexToLocFormat == 1;

			reader.Seek(maxpEntry.offset);
			var maxpData = ReadMaxpData(reader);

			reader.Seek(locaEntry.offset);
			var locations = ReadLocationData(reader, maxpData.numGlyphs, isLongVersion);

            reader.Seek(cmapEntry.offset);
			font.cmapData = ReadCmapData(reader);

            font.glyphData = new GlyphData[maxpData.numGlyphs - 1];
            for (int i = 0; i < font.glyphData.Length; i++)
            {
				if (locations[i + 1] - locations[i] <= 0) // Empty glyph
					continue;

                reader.Seek(glyphEntry.offset + locations[i]);
				font.glyphData[i] = ReadGlyphData(reader);
            }

			return font;
		}

		static uint[] ReadLocationData(TtfReader reader, ushort glyphs, bool isLongVersion)
		{
			uint[] offsets = new uint[glyphs];
            for (int i = 0; i < offsets.Length; i++)
            {
				offsets[i] = isLongVersion ? reader.ReadUInt32() : reader.ReadUInt16() * 2u;
            }

			return offsets;
        }

		static MaxpTable ReadMaxpData(TtfReader reader)
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

		static HeadTable ReadHeadData(TtfReader reader)
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

		static CmapData ReadCmapData(TtfReader reader)
		{
			long cmapBase = reader.Position;

			ushort version = reader.ReadUInt16();
			ushort subtableCount = reader.ReadUInt16();

			Span<CmapSubtable> subtables = stackalloc CmapSubtable[subtableCount];

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

		static CmapData ReadCmapFormat4(TtfReader reader)
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

			CmapData cmap = new CmapData();
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

		static GlyphData ReadGlyphData(TtfReader reader)
		{
			GlyphData glyphData = new();
			glyphData.glyphDescription = new()
			{
				numberOfContours = reader.ReadInt16(),
				xMin = reader.ReadUInt16(),
				yMin = reader.ReadUInt16(),
				xMax = reader.ReadUInt16(),
				yMax = reader.ReadUInt16()
			};

			if (glyphData.glyphDescription.numberOfContours < 0) // Countours, deal with later
				return glyphData;

			glyphData.endPtsOfContours = new ushort[glyphData.glyphDescription.numberOfContours];
            for (int i = 0; i < glyphData.endPtsOfContours.Length; i++)
            {
				glyphData.endPtsOfContours[i] = reader.ReadUInt16();
            }

			ushort instructionLength = reader.ReadUInt16();
			byte[] instructions = new byte[instructionLength];
			for (int i = 0; i < instructions.Length; i++)
			{
				instructions[i] = reader.ReadByte();
            }

			int numPoints = glyphData.endPtsOfContours[glyphData.glyphDescription.numberOfContours - 1] + 1;
			Flag[] flags = new Flag[numPoints];
            for (int i = 0; i < numPoints; i++)
            {
				Flag flag = (Flag)reader.ReadByte();
				flags[i] = flag;

				if (flags[i].HasFlag(Flag.Repeat))
				{
					byte rep = reader.ReadByte();
					for (int r = 0; r < rep; r++) flags[++i] = flag;
				}	
            }

			glyphData.xCoords = new int[numPoints];
			glyphData.yCoords = new int[numPoints];

			ReadCoords(reader, glyphData.xCoords, flags, Flag.XByte, Flag.XSignOrSame);
			ReadCoords(reader, glyphData.yCoords, flags, Flag.YByte, Flag.YSignOrSame);

            return glyphData;
		}

		static void ReadCoords(TtfReader reader, Span<int> array, Span<Flag> flags, Flag isByte, Flag signOrSame)
		{
			array[0] = 0;

			if (flags[0].HasFlag(isByte))
			{
				array[0] = reader.ReadByte() * (flags[0].HasFlag(signOrSame) ? 1 : -1);
			}
			else if(!flags[0].HasFlag(signOrSame))
			{
				array[0] = reader.ReadInt16();
			}

			for (int i = 1; i < array.Length; i++)
			{
				if (flags[i].HasFlag(isByte))
				{
					array[i] = array[i - 1] + reader.ReadByte() * (flags[i].HasFlag(signOrSame) ? 1 : -1);
				}
				else if(flags[i].HasFlag(signOrSame))
				{
					array[i] = array[i - 1];
				}
				else
				{
					array[i] = array[i - 1] + reader.ReadInt16();
				}
			}
		}
	}

	public static class Triangulation
	{
		public static Memory<int> Triangulate(Span<Vector2> verticies)
		{
			List<int> currentVerticies = new List<int>();
            for (int i = 0; i < verticies.Length; i++)
            {
				currentVerticies.Add(i);
            }

			List<int> indicies = new List<int>();
            while (currentVerticies.Count > 4)
            {
				for (int i = 0; i < currentVerticies.Count; i++)
				{
					int prevIdx = Loop(i - 1, currentVerticies.Count);
					int currIdx = Loop(i, currentVerticies.Count);
					int nextIdx = Loop(i + 1, currentVerticies.Count);

					Vector2 prev = verticies[currentVerticies[prevIdx]];
					Vector2 next = verticies[currentVerticies[nextIdx]];

					List<int> newList = new(currentVerticies);
					newList.Remove(currentVerticies[prevIdx]);
					newList.Remove(currentVerticies[currIdx]);
					newList.Remove(currentVerticies[nextIdx]);

					Span<int> listSpan = CollectionsMarshal.AsSpan(newList);
					if (IntersectAny(prev, next, listSpan, verticies))
					{
						indicies.Add(currentVerticies[prevIdx]);
						indicies.Add(currentVerticies[currIdx]);
						indicies.Add(currentVerticies[nextIdx]);

						currentVerticies.Remove(currentVerticies[currIdx]);
					}
				}
			}

			indicies.Add(currentVerticies[0]);
			indicies.Add(currentVerticies[1]);
			indicies.Add(currentVerticies[3]);

			indicies.Add(currentVerticies[1]);
			indicies.Add(currentVerticies[2]);
			indicies.Add(currentVerticies[3]);

			return indicies.ToArray();
        }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static int Loop(int idx, int modulo)
		{
			return (idx + modulo) % modulo;
		}

		static bool IntersectAny(Vector2 p1, Vector2 p2, Span<int> indicies, Span<Vector2> verticies)
		{
			if (indicies.Length <= 1)
				return false;

            for (int i = 0; i < indicies.Length - 1; i++)
            {
				if (Intersect(p1, p2, verticies[indicies[i]], verticies[indicies[i + 1]]))
					return true;
            }

			return false;
        }

		public static bool Intersect(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
		{
			int o1 = Orientation(p1, q1, p2);
			int o2 = Orientation(p1, q1, q2);
			int o3 = Orientation(p2, q2, p1);
			int o4 = Orientation(p2, q2, q1);

			if (o1 != o2 && o3 != o4)
				return true;

			// Special Cases 
			// p1, q1 and p2 are collinear and p2 lies on segment p1q1 
			if (o1 == 0 && OnSegment(p1, p2, q1)) return true;

			// p1, q1 and q2 are collinear and q2 lies on segment p1q1 
			if (o2 == 0 && OnSegment(p1, q2, q1)) return true;

			// p2, q2 and p1 are collinear and p1 lies on segment p2q2 
			if (o3 == 0 && OnSegment(p2, p1, q2)) return true;

			// p2, q2 and q1 are collinear and q1 lies on segment p2q2 
			if (o4 == 0 && OnSegment(p2, q1, q2)) return true;

			return false; // Doesn't fall in any of the above cases 
		}

		static int Orientation(Vector2 p, Vector2 q, Vector2 r)
		{
			// See https://www.geeksforgeeks.org/orientation-3-ordered-points/ 
			// for details of below formula. 
			int val = (int)MathF.Round((q.Y - p.X) * (r.X - q.X) - (q.X - p.X) * (r.Y - q.Y));

			if (val == 0) return 0; // collinear 

			return (val > 0) ? 1 : 2; // clock or counterclock wise 
		}

		static Boolean OnSegment(Vector2 p, Vector2 q, Vector2 r)
		{
			if (q.X <= Math.Max(p.X, r.X) && q.X >= Math.Min(p.X, r.X) &&
				q.Y <= Math.Max(p.Y, r.Y) && q.Y >= Math.Min(p.Y, r.Y))
				return true;

			return false;
		}
	}
}
