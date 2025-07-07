using EnCS;
using EnCS.Attributes;
using Hengine.Translation;
using Hengine.Utils.Parsing.TTF;
using Hengine.Utils;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;
using System.Runtime.InteropServices;
using System.Buffers;
using Microsoft.Extensions.Logging;
using UtilLib.Span;

namespace Hengine.Graphics
{
	record struct GlyphRange(int start, int length, bool clockwise);

	public struct VkTextBuffer
	{
		public Buffer vertexBuffer;
		public DeviceMemory vertexBufferMemory;

		public uint indicies;
		public Buffer indexBuffer;
		public DeviceMemory indexBufferMemory;
	}

	[ResourceManager]
	public partial class VulkanTextResourceManager : IResourceManager<GuiText, VkTextBuffer>
	{
		uint idx = 0;
		Memory<Graphics.VkTextBuffer> meshBuffers = new Graphics.VkTextBuffer[32];

		Dictionary<string, uint> meshCache = new Dictionary<string, uint>();

		ILogger logger;
		VkContext context;
		TranslationManager translationManager;

		public VulkanTextResourceManager(ILoggerFactory factory, VkContext context, TranslationManager translationManager)
		{
			this.logger = factory.CreateLogger<VulkanMaterialResourceManager>();
			this.context = context;
			this.translationManager = translationManager;
		}

		public ref VkTextBuffer Get(uint id)
		{
			return ref meshBuffers.Span[(int)id];
		}

		public uint Store(in Hengine.Graphics.GuiText resource)
		{
			if (meshCache.TryGetValue(resource.id, out uint id))
				return id;

			logger.LogResourceManagerStore(resource.id);

			meshCache.Add(resource.id, idx);
			meshBuffers.Span[(int)idx] = CreateTextBuffer(context, translationManager, resource);
			return idx++;
		}

		static Graphics.VkTextBuffer CreateTextBuffer(VkContext context, TranslationManager translationManager, in Graphics.GuiText resource)
		{
			string str = translationManager.GetTranslation(resource.id);

			using var vertBuff = MemoryPool<Vector2f>.Shared.Rent(10000);
			SpanList<Vector2f> verticies = vertBuff.Memory.Span;

			using var indexBuff = MemoryPool<int>.Shared.Rent(10000);
			SpanList<int> indicies = indexBuff.Memory.Span;

			int offset = 0;
			int advanced = 0;
			for (int i = 0; i < str.Length; i++)
			{
				var glyph = resource.font.GetGlyphIndex(str[i]);
				var advance = resource.font.GetGlyphAdvance(str[i]);
				if (advance < 100) // TODO: Make less hacky, this is for space.
					advance = 1200;

                var meshes = ProcessMesh(glyph);
				for (int a = 0; a < meshes.Count; a++)
				{
					Memory<int> newIndicies = Triangulation.Triangulate(meshes[a].Span);

					for (int b = 0; b < newIndicies.Length; b++)
						newIndicies.Span[b] += offset;

					for (int b = 0; b < meshes[a].Span.Length; b++)
						meshes[a].Span[b] += new Vector2f(advanced, 0);

					verticies.Add(meshes[a].Span);
					indicies.Add(newIndicies.Span);

					offset += meshes[a].Length;
				}

				advanced += advance;
			}

			var vertMemory = MemoryPool<GuiVertex>.Shared.Rent(verticies.Count);
			var indexMemory = MemoryPool<ushort>.Shared.Rent(indicies.Count);

			Vector2f delta = new Vector2f(1000, 1000);
			for (int i = 0; i < verticies.Count; i++)
				vertMemory.Memory.Span[i] = new GuiVertex(new Vector4f(verticies[i].x / delta.x, 0, 1 - verticies[i].y / delta.y, 0), Vector2f.Zero);

			for (int i = 0; i < indicies.Count; i++)
				indexMemory.Memory.Span[i] = (ushort)indicies[i];

			var meshBuffer = VulkanMeshResourceManager.CreateBuffer(context, vertMemory.Memory.Span.Slice(0, verticies.Count), indexMemory.Memory.Span.Slice(0, indicies.Count), GuiVertex.SizeInBytes);

			return new()
			{
				vertexBuffer = meshBuffer.vertexBuffer,
				vertexBufferMemory = meshBuffer.vertexBufferMemory,
				indicies = meshBuffer.indicies,
				indexBuffer = meshBuffer.indexBuffer,
				indexBufferMemory = meshBuffer.indexBufferMemory,
			};
		}

		public static List<Memory<Vector2f>> ProcessMesh(in GlyphData glyphData)
		{
			if (glyphData.contours.Length == 0)
				return new List<Memory<Vector2f>>();

			scoped Span<Vector2f> coords = stackalloc Vector2f[glyphData.coords.Length];
			for (int i = 0; i < glyphData.coords.Length; i++)
			{
				coords[i] = new (glyphData.coords.Span[i].x, glyphData.coords.Span[i].y);
			}

			scoped Span<GlyphRange> glyphRanges = stackalloc GlyphRange[glyphData.contours.Length];
			GetGlyphRanges(glyphData, glyphRanges, coords);

			var meshes = new List<Memory<Vector2f>>();
			using var buff = MemoryPool<Vector2f>.Shared.Rent(glyphData.coords.Length);
			var buffSpan = buff.Memory.Span;

			for (int i = 0; i < glyphRanges.Length; i++)
			{
				int glyphs = FindNextMesh(glyphRanges.Slice(i + 1));
				int meshLength = GetMeshSize(glyphRanges.Slice(i, glyphs + 1));

				meshes.Add(new Vector2f[meshLength]);
				var meshSpan = meshes[meshes.Count - 1].Span;

				AddMeshHoles(meshSpan, glyphRanges.Slice(i, glyphs + 1), coords, buffSpan);

				i += glyphs; // Skip to next non hole mesh.
			}

			return meshes;
		}

		static void GetGlyphRanges(in GlyphData glyphData, scoped Span<GlyphRange> glyphs, scoped ReadOnlySpan<Vector2f> coords)
		{
			int totalMeshes = 0;
			int prev = 0;
			for (int i = 0; i < glyphData.contours.Length; i++)
			{
				int idx = glyphData.contours.Span[i] + 1;
				int length = idx - prev;

				var slice = coords.Slice(prev, length);
				bool clockwise = VectorMath.IsClockwise(slice);

				if (clockwise)
					totalMeshes++;

				glyphs[i] = new GlyphRange(prev, length, clockwise);
				prev = idx;
			}
		}

		static void AddMeshHoles(scoped Span<Vector2f> mesh, scoped ReadOnlySpan<GlyphRange> glyphRanges, scoped ReadOnlySpan<Vector2f> coords, scoped Span<Vector2f> buff)
		{
			int start = glyphRanges[0].start;
			int length = glyphRanges[0].length;
			coords.Slice(start, length).CopyTo(mesh);

			int currLength = length;
			for (int a = 1; a < glyphRanges.Length; a++)
			{
				int holeStart = glyphRanges[a].start;
				int holeLength = glyphRanges[a].length;

				Triangulation.AddHole(mesh.Slice(0, currLength), coords.Slice(holeStart, holeLength), buff);

				currLength += glyphRanges[a].length + 2;
				buff.Slice(0, currLength).CopyTo(mesh);
			}
		}

		static int GetMeshSize(scoped ReadOnlySpan<GlyphRange> span)
		{
			int size = span[0].length; // First range is always skin.
			for (int i = 1; i < span.Length; i++)
			{
				size = Triangulation.GetMeshWithHoleSize(size, span[i].length);
			}

			return size;
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
