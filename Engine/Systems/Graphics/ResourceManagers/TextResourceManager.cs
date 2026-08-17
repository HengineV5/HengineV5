using EnCS;
using EnCS.Attributes;
using Engine.Translation;
using Engine.Utils;
using Engine.Utils.Parsing.TTF;
using Microsoft.Extensions.Logging;
using System.Buffers;
using UtilLib.Extensions;
using UtilLib.Span;

using RenderLib;

namespace Engine.Graphics
{
	record struct GlyphRange(int start, int length, bool clockwise);

	public struct TextBuffer
	{
		public GpuBuffer vertexBuffer;

		public uint indicies;
		public GpuBuffer indexBuffer;

		public Font font;
		public Memory<int> vertexOffsets;
		public Memory<int> indexOffsets;
		public Memory<float> advances;
	}

	[ResourceManager]
	public partial class TextResourceManager : IResourceManager<GuiText, TextBuffer>
	{
		uint idx = 0;
		Memory<Graphics.TextBuffer> meshBuffers = new Graphics.TextBuffer[32];

		Dictionary<string, uint> meshCache = new Dictionary<string, uint>();

		ILogger logger;
		GraphicsContext renderContext;
		TranslationManager translationManager;

		public TextResourceManager(ILoggerFactory factory, GraphicsContext renderContext, TranslationManager translationManager)
		{
			this.logger = factory.CreateLogger<MaterialResourceManager>();
			this.renderContext = renderContext;
			this.translationManager = translationManager;
		}

		public ref TextBuffer Get(uint id)
		{
			return ref meshBuffers.Span[(int)id];
		}

		public uint Store(in Engine.Graphics.GuiText resource)
		{
			if (meshCache.TryGetValue(resource.id, out uint id))
				return id;

			logger.LogResourceManagerStore(resource.id);

			meshCache.Add(resource.id, idx);
			//meshBuffers.Span[(int)idx] = CreateTextBuffer(ref backend, translationManager, resource);
			var backend = renderContext.CreateBackend();
			meshBuffers.Span[(int)idx] = CreateFontBuffer(ref backend, resource.font);
			return idx++;
		}

		static Graphics.TextBuffer CreateFontBuffer<TBackend>(ref TBackend backend, Font font) where TBackend : struct, IRenderBackend<TBackend>, allows ref struct
		{
			using var vertBuff = MemoryPool<Vector2f>.Shared.Rent(1000000);
			using var uvBuff = MemoryPool<Vector3f>.Shared.Rent(1000000);
			using var indexBuff = MemoryPool<int>.Shared.Rent(1000000);

			using var vertexOffsetBuff = MemoryPool<int>.Shared.Rent(1000000);
			using var indexOffsetBuff = MemoryPool<int>.Shared.Rent(1000000);

			SpanList<Vector2f> verticies = vertBuff.Memory.Span;
			SpanList<Vector3f> uvs = uvBuff.Memory.Span;
			SpanList<int> indicies = indexBuff.Memory.Span;

			SpanList<int> vertexOffsets = vertexOffsetBuff.Memory.Span;
			SpanList<int> indexOffsets = indexOffsetBuff.Memory.Span;

			int failed = 0;
			int skipped = 0;
			for (int i = 0; i < font.Glyphs.Length; i++)
			{
				ref readonly var glyph = ref font.Glyphs[i];

				if (glyph.coords.Length < 3)
				{
                    vertexOffsets.Add(verticies.Count);
                    indexOffsets.Add(indicies.Count);
                    //Console.WriteLine($"Mesh {i} skipped");
					skipped++;
                    continue; // Skip empty glyphs.
				}

                vertexOffsets.Add(verticies.Count);
                indexOffsets.Add(indicies.Count);

                if (!TryProcessMesh(glyph, ref verticies, ref uvs, ref indicies, 0))
				{
					failed++;
					//Console.WriteLine($"Mesh {i} failed");
				}
			}
			/*
			Console.WriteLine($"{failed}/{font.Glyphs.Length} failed");
			Console.WriteLine($"{skipped}/{font.Glyphs.Length} skipped");
			*/

			/*
			ref readonly var glyph = ref font.Glyphs[2291];
			//ref readonly var glyph = ref font.GetUnicodeGlyph('E');
			bool s = TryProcessMesh(glyph, ref verticies, ref uvs, ref indicies, 0);
			Console.WriteLine($"Success: {s}");
			throw new Exception($"Yay!");
			*/

            using var vertMemory = MemoryPool<GuiVertex>.Shared.Rent(verticies.Count);
			using var indexMemory = MemoryPool<ushort>.Shared.Rent(indicies.Count);

			Vector2f delta = new Vector2f(1000, 1000);
			delta *= 3;

			//delta.x = font.Head.xMax;
			//delta.y = font.Head.yMax;

			float scaling = (18f) / (72f * font.Head.unitsPerEm);
			//float scaling = (18f * 72f) / (72f * 10 * font.Head.unitsPerEm);
			//float scaling = 1f / font.Head.unitsPerEm;
			//scaling *= 0.5f;

            for (int i = 0; i < verticies.Count; i++)
				//vertMemory.Memory.Span[i] = new GuiVertex(new Vector4f(verticies[i].x / delta.x, 0, 1 - verticies[i].y / delta.y, 0), new(uvs[i].x, uvs[i].y), uvs[i].z == 1);
				vertMemory.Memory.Span[i] = new GuiVertex(new Vector4f(verticies[i].x, 0, -verticies[i].y, 0) * scaling, new(uvs[i].x, uvs[i].y), uvs[i].z == 1);

			for (int i = 0; i < indicies.Count; i++)
				indexMemory.Memory.Span[i] = (ushort)indicies[i];

			var meshBuffer = MeshBufferFactory.CreateBuffer(ref backend, vertMemory.Memory.Span[..verticies.Count], indexMemory.Memory.Span[..indicies.Count]);

			var vOffsets = new int[vertexOffsets.Count];
			vertexOffsets.AsSpan().CopyTo(vOffsets);

			var iOffsets = new int[indexOffsets.Count];
			indexOffsets.AsSpan().CopyTo(iOffsets);

			//scaling *= 2f;

			var advances = new float[font.Glyphs.Length];
			for (int i = 0; i < font.Glyphs.Length; i++)
				advances[i] = font.HorizontalMetrics[i].advanceWidth * scaling * 100f;

            return new()
			{
				vertexBuffer = meshBuffer.vertexBuffer,
				indicies = meshBuffer.indicies,
				indexBuffer = meshBuffer.indexBuffer,

				font = font,
				vertexOffsets = vOffsets,
				indexOffsets = iOffsets,
				advances = advances,
            };
		}

		static Graphics.TextBuffer CreateTextBuffer<TBackend>(ref TBackend backend, TranslationManager translationManager, in Graphics.GuiText resource) where TBackend : struct, IRenderBackend<TBackend>, allows ref struct
		{
			string str = translationManager.GetTranslation(resource.id);

			using var vertBuff = MemoryPool<Vector2f>.Shared.Rent(10000);
			SpanList<Vector2f> verticies = vertBuff.Memory.Span;

			using var uvBuff = MemoryPool<Vector3f>.Shared.Rent(10000);
			SpanList<Vector3f> uvs = uvBuff.Memory.Span;

			using var indexBuff = MemoryPool<int>.Shared.Rent(10000);
			SpanList<int> indicies = indexBuff.Memory.Span;

			int advanced = 0;
			for (int i = 0; i < str.Length; i++)
			{
				var glyph = resource.font.GetUnicodeGlyph(str[i]);
				var toAdvance = resource.font.GetGlyphAdvance(str[i]);
				if (toAdvance < 100) // TODO: Make less hacky, this is for space.
					toAdvance = 1200;

				if (glyph.contours.Length == 0)
				{
					advanced += toAdvance;
					continue;
				}

				TryProcessMesh(glyph, ref verticies, ref uvs, ref indicies, advanced);

				advanced += toAdvance;
			}

			using var vertMemory = MemoryPool<GuiVertex>.Shared.Rent(verticies.Count);
			using var indexMemory = MemoryPool<ushort>.Shared.Rent(indicies.Count);

			float scale = 1;

			Vector2f delta = new Vector2f(1000, 1000);
			for (int i = 0; i < verticies.Count; i++)
				vertMemory.Memory.Span[i] = new GuiVertex(new Vector4f(verticies[i].x / delta.x, 0, 1 - verticies[i].y / delta.y, 0) * scale, new(uvs[i].x, uvs[i].y), uvs[i].z == 1);

			for (int i = 0; i < indicies.Count; i++)
				indexMemory.Memory.Span[i] = (ushort)indicies[i];

			var meshBuffer = MeshBufferFactory.CreateBuffer(ref backend, vertMemory.Memory.Span[..verticies.Count], indexMemory.Memory.Span[..indicies.Count]);

			return new()
			{
				vertexBuffer = meshBuffer.vertexBuffer,
				indicies = meshBuffer.indicies,
				indexBuffer = meshBuffer.indexBuffer,
			};
		}

		static bool TryProcessMesh(in GlyphData glyphData, scoped ref SpanList<Vector2f> verticeis, scoped ref SpanList<Vector3f> uvs, scoped ref SpanList<int> indicies, int advance)
		{
			scoped Span<GlyphRange> glyphRanges = stackalloc GlyphRange[glyphData.contours.Length];
			if (!TryGetGlyphRanges(glyphData, glyphRanges))
				return false;

            using var buff = MemoryPool<Vector2f>.Shared.Rent(2048);
			var buffSpan = buff.Memory.Span;

			for (int i = 0; i < glyphRanges.Length; i++)
			{
				int glyphs = FindNextMesh(glyphRanges.Slice(i + 1));
				int innerMeshLength = GetInnerMeshSize(glyphRanges.Slice(i, glyphs + 1), glyphData.coords.Span);
				int curveMeshLength = GetCurveMeshSize(glyphRanges.Slice(i, glyphs + 1), glyphData.coords.Span);

				int innerOffset = verticeis.Count;
				var innerMeshSpan = verticeis.Reserve(innerMeshLength);
				uvs.Reserve(innerMeshLength);

				int curveOffset = verticeis.Count;
				var curveMeshSpan = verticeis.Reserve(curveMeshLength);
				var curvedUvsSpan = uvs.Reserve(curveMeshLength);

				CreateInnerMesh(innerMeshSpan, glyphRanges.Slice(i, glyphs + 1), glyphData.coords.Span, buffSpan);
				CreateCurveMesh(curveMeshSpan, curvedUvsSpan, glyphRanges.Slice(i, glyphs + 1), glyphData.coords.Span);

				if (!Triangulation.TryTriangulate(innerMeshSpan, out Memory<int> newIndicies))
				{
					verticeis.Free(innerMeshSpan.Length);
					verticeis.Free(curveMeshSpan.Length);

					uvs.Free(innerMeshLength);
					uvs.Free(curvedUvsSpan.Length);

					return false;
				}

				var indiciesSpan = indicies.Reserve(newIndicies.Length);
				newIndicies.Span.CopyTo(indiciesSpan);

				indiciesSpan.ForEach((ref int i) => i += innerOffset);
				innerMeshSpan.ForEach((ref Vector2f i) => i += new Vector2f(advance, 0));

				var curveIndiciesSpan = indicies.Reserve(curveMeshLength);
				CreateCurveMeshIndicies(curveMeshSpan, curveIndiciesSpan, curveOffset);
				curveMeshSpan.ForEach((ref Vector2f i) => i += new Vector2f(advance, 0));

				i += glyphs; // Skip to next non hole mesh.
			}

			return true;
		}

		static bool TryGetGlyphRanges(in GlyphData glyphData, scoped Span<GlyphRange> glyphs)
		{
			scoped Span<Vector2f> coords = stackalloc Vector2f[glyphData.coords.Length];
			for (int i = 0; i < glyphData.coords.Length; i++)
			{
				coords[i] = new(glyphData.coords.Span[i].x, glyphData.coords.Span[i].y);
			}

			(Vector2f min, Vector2f max) bounds = new();

            int prev = 0;
			for (int i = 0; i < glyphData.contours.Length; i++)
			{
				int idx = glyphData.contours.Span[i] + 1;
				int length = idx - prev;

				if (idx > glyphData.coords.Length)
					return false;

                var slice = coords.Slice(prev, length);
				bool clockwise = VectorMath.IsClockwise(slice);

				if (clockwise)
				{
					bounds = GetBounds(slice);
				}
				else
				{
                    (Vector2f min, Vector2f max) = GetBounds(slice);

                    // If contour is wound cc it is suppose to be a hole.
                    // If the hole is outside the previous skin it has been wound wrong.
					// Fix this
                    if (!(min.x < bounds.max.x && min.y < bounds.max.y && max.x > bounds.min.x && max.y > bounds.min.y))
					{
                        glyphData.coords.Span.Slice(prev, length).Reverse();
						clockwise = true;
                    }
                }

                glyphs[i] = new GlyphRange(prev, length, clockwise);
				prev = idx;
			}

			return true;
		}

		static (Vector2f min, Vector2f max) GetBounds(scoped ReadOnlySpan<Vector2f> verticeis)
		{
			Vector2f min = new(float.MaxValue, float.MaxValue);
			Vector2f max = new(float.MinValue, float.MinValue);

			for (int i = 0; i < verticeis.Length; i++)
			{
				min.x = MathF.Min(min.x, verticeis[i].x);
				min.y = MathF.Min(min.y, verticeis[i].y);

				max.x = MathF.Max(max.x, verticeis[i].x);
				max.y = MathF.Max(max.y, verticeis[i].y);
            }

			return (min, max);
		}

		static int CreateInnerMesh(scoped Span<Vector2f> mesh, scoped ReadOnlySpan<GlyphRange> glyphRanges, scoped ReadOnlySpan<GlyphVertex> vertices, scoped Span<Vector2f> buff)
		{
			scoped Span<Vector2f> buff2 = stackalloc Vector2f[128];

			int currLength = CopyInnerMesh(in glyphRanges[0], vertices, mesh);
			for (int i = 1; i < glyphRanges.Length; i++)
			{
				buff2.Clear();
				int holeLength = CopyInnerMesh(in glyphRanges[i], vertices, buff2);

				Triangulation.AddHole(mesh.Slice(0, currLength), buff2.Slice(0, holeLength), buff);

				currLength += holeLength + 2;
				buff.Slice(0, currLength).CopyTo(mesh);
			}

			return currLength;
		}

		static int CopyInnerMesh(ref readonly GlyphRange range, scoped ReadOnlySpan<GlyphVertex> vertices, scoped Span<Vector2f> mesh)
		{
			scoped ReadOnlySpanRingBuffer<GlyphVertex> section = vertices.Slice(range.start, range.length);
			scoped SpanList<Vector2f> meshBuilder = mesh;

			for (int i = 0; i < range.length; i++)
			{
				ref readonly var prev = ref section[i - 1];
				ref readonly var curr = ref section[i];
				ref readonly var next = ref section[i + 1];

				if (curr.onCurve && next.onCurve)
				{
					meshBuilder.Add(new Vector2f(next.x, next.y));
				}
				else if (!curr.onCurve)
				{
					(Vector2f prevPos, Vector2f currPos, Vector2f nextPos) = GetInnerTriangle(in prev, in curr, in next);

					var ab = currPos - prevPos;
					var ac = nextPos - prevPos;
					ac = new(ac.y, -ac.x); // Create orthogonal vector

					var dot = Vector2f.Dot(in ab, in ac);

					if (dot > 0)
						meshBuilder.Add(currPos);

					meshBuilder.Add(nextPos);
				}
			}

			return meshBuilder.Count;
		}

		static (Vector2f, Vector2f, Vector2f) GetInnerTriangle(ref readonly GlyphVertex prev, ref readonly GlyphVertex curr, ref readonly GlyphVertex next)
		{
			Vector2f prevPos = new Vector2f(prev.x, prev.y);
			Vector2f currPos = new Vector2f(curr.x, curr.y);
			Vector2f nextPos = new Vector2f(next.x, next.y);

			if (!curr.onCurve && !next.onCurve)
			{
				Vector2f midNext = currPos + (nextPos - currPos) / 2f;

				return (prevPos, currPos, midNext);
			}
			else if (!curr.onCurve && next.onCurve)
			{
				return (prevPos, currPos, nextPos);
			}

			return (prevPos, currPos, nextPos);
		}

		static int CreateCurveMesh(scoped Span<Vector2f> mesh, scoped Span<Vector3f> uvs, scoped ReadOnlySpan<GlyphRange> glyphRanges, scoped ReadOnlySpan<GlyphVertex> vertices)
		{
			scoped SpanList<Vector2f> meshBuilder = mesh;
			scoped SpanList<Vector3f> uvBuilder = uvs;

			int length = 0;
			for (int i = 0; i < glyphRanges.Length; i++)
			{
				scoped ReadOnlySpanRingBuffer<GlyphVertex> section = vertices.Slice(glyphRanges[i].start, glyphRanges[i].length);

				length += CopyCurveMesh(ref meshBuilder, ref uvBuilder, in glyphRanges[i], section);
			}

			return length;
		}

		static int CopyCurveMesh(scoped ref SpanList<Vector2f> mesh, scoped ref SpanList<Vector3f> uvs, ref readonly GlyphRange range, scoped ReadOnlySpanRingBuffer<GlyphVertex> section)
		{
			for (int i = 0; i < range.length; i++)
			{
				ref readonly var prev = ref section[i - 1];
				ref readonly var curr = ref section[i];
				ref readonly var next = ref section[i + 1];

				if (curr.onCurve)
					continue;

				Vector2f prevPos = new Vector2f(prev.x, prev.y);
				Vector2f currPos = new Vector2f(curr.x, curr.y);
				Vector2f nextPos = new Vector2f(next.x, next.y);

				Vector2f midPrev = prevPos + (currPos - prevPos) / 2f;
				Vector2f midNext = currPos + (nextPos - currPos) / 2f;

				mesh.Add(prev.onCurve ? prevPos : midPrev);
				mesh.Add(currPos);
				mesh.Add(next.onCurve ? nextPos : midNext);

				var ab = currPos - prevPos;
				var ac = nextPos - prevPos;
				ac = new(ac.y, -ac.x); // Create orthogonal vector

				var dot = Vector2f.Dot(in ab, in ac);
				bool inverted = dot > 0;

				uvs.Add(new Vector3f(0, 0, inverted ? 1 : 0));
				uvs.Add(new Vector3f(0.5f, 0, inverted ? 1 : 0));
				uvs.Add(new Vector3f(1, 1, inverted ? 1 : 0));
			}

			return uvs.Count;
		}

		static void CreateCurveMeshIndicies(scoped ReadOnlySpan<Vector2f> mesh, scoped Span<int> indices, int offset)
		{
			for (int i = 0; i < mesh.Length; i++)
			{
				indices[i] = i + offset;
			}
		}

		static int GetInnerMeshSize(scoped ReadOnlySpan<GlyphRange> glyphRanges, scoped ReadOnlySpan<GlyphVertex> vertices)
		{
			int size = GetInnerLength(in glyphRanges[0], vertices); // First range is always skin.
			for (int i = 1; i < glyphRanges.Length; i++)
			{
				size = Triangulation.GetMeshWithHoleSize(size, GetInnerLength(in glyphRanges[i], vertices));
			}

			return size;
		}

		static int GetCurveMeshSize(scoped ReadOnlySpan<GlyphRange> glyphRanges, scoped ReadOnlySpan<GlyphVertex> vertices)
		{
			int size = 0;
			for (int i = 0; i < glyphRanges.Length; i++)
			{
				size += GetCurveMeshLength(in glyphRanges[i], vertices);
			}

			return size;
		}

		static int GetInnerLength(ref readonly GlyphRange range, scoped ReadOnlySpan<GlyphVertex> vertices)
		{
			scoped ReadOnlySpanRingBuffer<GlyphVertex> section = vertices.Slice(range.start, range.length);

			int length = 0;
			for (int i = 0; i < range.length; i++)
			{
				ref readonly var prev = ref section[i - 1];
				ref readonly var curr = ref section[i];
				ref readonly var next = ref section[i + 1];

				if (curr.onCurve && next.onCurve)
				{
					length++;
				}
				else if (!curr.onCurve)
				{
					length++;

					(Vector2f prevPos, Vector2f currPos, Vector2f nextPos) = GetInnerTriangle(in prev, in curr, in next);

					var ab = currPos - prevPos;
					var ac = nextPos - prevPos;
					ac = new(ac.y, -ac.x); // Create orthogonal vector

					var dot = Vector2f.Dot(in ab, in ac);

					if (dot > 0)
						length++;
				}
			}

			return length;
		}

		static int GetCurveMeshLength(ref readonly GlyphRange range, scoped ReadOnlySpan<GlyphVertex> vertices)
		{
			scoped ReadOnlySpanRingBuffer<GlyphVertex> section = vertices.Slice(range.start, range.length);

			int length = 0;
			for (int i = 0; i < range.length; i++)
			{
				ref readonly var curr = ref section[i];

				if (!curr.onCurve)
					length += 3;
			}

			return length;
		}

		static int FindNextMesh(scoped ReadOnlySpan<GlyphRange> span)
		{
			for (int i = 0; i < span.Length; i++)
			{
				if (span[i].clockwise)
					return i;
			}

			return span.Length;
		}
	}
}
