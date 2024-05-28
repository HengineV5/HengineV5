using EnCS;
using EnCS.Attributes;
using Engine.Translation;
using Engine.Utils.Parsing.TTF;
using Engine.Utils;
using Silk.NET.Vulkan;
using System.Numerics;
using Buffer = Silk.NET.Vulkan.Buffer;
using System.Runtime.InteropServices;
using System.Buffers;

namespace Engine.Graphics
{
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

		VkContext context;
		TranslationManager translationManager;

		public VulkanTextResourceManager(VkContext context, TranslationManager translationManager)
		{
			this.context = context;
			this.translationManager = translationManager;
		}

		public ref VkTextBuffer Get(uint id)
		{
			return ref meshBuffers.Span[(int)id];
		}

		public uint Store(in Engine.Graphics.GuiText resource)
		{
			if (meshCache.TryGetValue(resource.id, out uint id))
				return id;

			meshCache.Add(resource.id, idx);
			meshBuffers.Span[(int)idx] = CreateTextBuffer(context, translationManager, resource);
			return idx++;
		}

		static Graphics.VkTextBuffer CreateTextBuffer(VkContext context, TranslationManager translationManager, in Graphics.GuiText resource)
		{
			string str = translationManager.GetTranslation(resource.id);

			List<Vector2> verticies = new List<Vector2>();
			List<int> indicies = new List<int>();

			int offset = 0;
			int advanced = 0;
			for (int i = 0; i < str.Length; i++)
			{
				var glyph = resource.font.GetGlyphIndex(str[i]);
				var advance = resource.font.GetGlyphAdvance(str[i]);
				if (advance < 100) // TODO: Make less hacky
					advance = 1200;

                var meshes = ProcessMesh(glyph);
				for (int a = 0; a < meshes.Count; a++)
				{
					Memory<int> newIndicies = Triangulation.Triangulate(meshes[a].Span);

					for (int b = 0; b < newIndicies.Length; b++)
						newIndicies.Span[b] += offset;

					for (int b = 0; b < meshes[a].Span.Length; b++)
						meshes[a].Span[b] += new Vector2(advanced, 0);

					verticies.AddRange(meshes[a].Span);
					indicies.AddRange(newIndicies.Span);

					offset += meshes[a].Length;
				}

				advanced += advance;
			}

			var vertMemory = MemoryPool<GuiVertex>.Shared.Rent(verticies.Count);
			var indexMemory = MemoryPool<ushort>.Shared.Rent(indicies.Count);

			Vector2 delta = new Vector2(1000, 1000);
			for (int i = 0; i < verticies.Count; i++)
				vertMemory.Memory.Span[i] = new GuiVertex(new Vector4(verticies[i].X / delta.X, 0, 1 - verticies[i].Y / delta.Y, 0), Vector2.Zero);

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

		public static List<Memory<Vector2>> ProcessMesh(in GlyphData glyphData)
		{
			if (glyphData.endPtsOfContours is null)
				return new List<Memory<Vector2>>();

			int offset = 0;
			List<Memory<Vector2>> meshes = new List<Memory<Vector2>>();
			while (offset != glyphData.endPtsOfContours.Length)
			{
				Memory<Vector2> mesh = new Vector2[0];
				offset = ProcessMeshWithHoles(glyphData, offset, ref mesh);

				meshes.Add(mesh);
			}

			return meshes;
		}

		static int ProcessMeshWithHoles(in GlyphData glyphData, int offset, ref Memory<Vector2> mesh)
		{
			// Assume first countour is always skin.

			if (offset == 0)
			{
				mesh = new Vector2[glyphData.endPtsOfContours[offset] + 1];
				for (int i = 0; i < mesh.Length; i++)
				{
					mesh.Span[i] = new Vector2(glyphData.xCoords[i], glyphData.yCoords[i]);
				}
			}
			else
			{
				int start = glyphData.endPtsOfContours[offset - 1] + 1;
				int end = glyphData.endPtsOfContours[offset] + 1;

				mesh = new Vector2[end - start];
				for (int i = 0; i < mesh.Length; i++)
				{
					mesh.Span[i] = new Vector2(glyphData.xCoords[start + i], glyphData.yCoords[start + i]);
				}
			}

			for (int i = offset + 1; i < glyphData.endPtsOfContours.Length; i++)
			{
				int start = glyphData.endPtsOfContours[i - 1] + 1;
				int end = glyphData.endPtsOfContours[i] + 1;

				Memory<Vector2> newMesh = new Vector2[end - start];
				for (int a = 0; a < newMesh.Length; a++)
				{
					newMesh.Span[a] = new Vector2(glyphData.xCoords[start + a], glyphData.yCoords[start + a]);
				}

				// If clockwise assume separate piece, else assume a hole.
				if (!VectorMath.IsClockwise(newMesh.Span))
					mesh = Triangulation.ProcessHole(mesh.Span, newMesh.Span);
				else
					return i;
			}

			return glyphData.endPtsOfContours.Length;
		}
	}
}
