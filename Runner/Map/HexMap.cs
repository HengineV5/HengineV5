using CommunityToolkit.HighPerformance;
using EnCS;
using Hengine.Graphics;
using System.Buffers;
using System.Runtime.CompilerServices;
using static Hengine.HengineEcs;

namespace Runner
{
	public ref struct HexNeighbors<T> where T : unmanaged
	{
		Span<T> neighbors;

		public ref T TopRight => ref neighbors[0];

		public ref T Right => ref neighbors[1];

		public ref T BottomRight => ref neighbors[2];

		public ref T BottomLeft => ref neighbors[3];

		public ref T Left => ref neighbors[4];

		public ref T TopLeft => ref neighbors[5];

		public HexNeighbors(Span<T> neighbors)
		{
			this.neighbors = neighbors;
		}
	}

	public ref struct MeshBuilder<TVert, TInd> where TVert : unmanaged where TInd : unmanaged
	{
		public Span<TVert> verticies;
		public Span<TInd> indicies;

		public int VertexOffset => vertOffset;

		public int IndexOffset => indexOffset;

		int vertOffset;
		int indexOffset;

        public MeshBuilder(Span<TVert> verticies, Span<TInd> indicies)
        {
			this.verticies = verticies;
			this.indicies = indicies;
        }

		public void AppendVertex(in TVert vert)
		{
			verticies[vertOffset] = vert;
			vertOffset = vertOffset + 1;
		}

		public void AppendIndex(in TInd index)
		{
			indicies[indexOffset] = index;
			indexOffset = indexOffset + 1;
		}

		public void AppendTriange(in TInd i1, in TInd i2, in TInd i3)
		{
			indicies[indexOffset + 0] = i1;
			indicies[indexOffset + 1] = i2;
			indicies[indexOffset + 2] = i3;
			indexOffset += 3;
		}
    }

	public static class MeshBuilderExtensions
	{
		public static void WriteToMesh(this MeshBuilder<Vertex, uint> meshBuilder, ref Mesh mesh)
		{
			mesh.verticies = new Vertex[meshBuilder.VertexOffset];
			mesh.indicies = new uint[meshBuilder.IndexOffset];

			meshBuilder.verticies.Slice(0, meshBuilder.VertexOffset).CopyTo(mesh.verticies);
			meshBuilder.indicies.Slice(0, meshBuilder.IndexOffset).CopyTo(mesh.indicies);
		}
	}

	static class HexMapBuilder
	{
		const float OUTER_RADIUS = 1f;
		const float INNER_RADIUS = OUTER_RADIUS * 0.866025404f;

		static Memory<Vector3f> HEX;

		static HexMapBuilder()
		{
			HEX = new Vector3f[6];

			HEX.Span[0] = new Vector3f(0, 0, OUTER_RADIUS);
			HEX.Span[1] = new Vector3f(INNER_RADIUS, 0, OUTER_RADIUS * 0.5f);
			HEX.Span[2] = new Vector3f(INNER_RADIUS, 0, -OUTER_RADIUS * 0.5f);
			HEX.Span[3] = new Vector3f(0, 0, -OUTER_RADIUS);
			HEX.Span[4] = new Vector3f(-INNER_RADIUS, 0, -OUTER_RADIUS * 0.5f);
			HEX.Span[5] = new Vector3f(-INNER_RADIUS, 0, OUTER_RADIUS * 0.5f);
		}

		// Creates 1 verticies
		public static void CreateCenterVertex(ref MeshBuilder<Vertex, uint> builder, in Vector3f pos, in Vector2f uv)
		{
			builder.AppendVertex(new Vertex(pos, Vector3f.UnitY, uv, Vector3f.UnitX));
		}

		// Creates 6 verticies
		public static void CreateHexVerticies(ref MeshBuilder<Vertex, uint> builder, in Vector3f pos, in Vector3f scale, in Vector2f uv, float neighborScale, scoped Span<Vector3f> neighbors)
		{
			Vector3f p0 = HEX.Span[0] * scale;
			Vector3f p1 = HEX.Span[1] * scale;
			Vector3f p2 = HEX.Span[2] * scale;
			Vector3f p3 = HEX.Span[3] * scale;
			Vector3f p4 = HEX.Span[4] * scale;
			Vector3f p5 = HEX.Span[5] * scale;

			HexNeighbors<Vector3f> hexNeighbors = new(neighbors);

			builder.AppendVertex(new Vertex(p0 + pos + hexNeighbors.TopRight * neighborScale, Vector3f.UnitY, uv, Vector3f.UnitX));
			builder.AppendVertex(new Vertex(p1 + pos + hexNeighbors.Right * neighborScale, Vector3f.UnitY, uv, Vector3f.UnitX));
			builder.AppendVertex(new Vertex(p2 + pos + hexNeighbors.BottomRight * neighborScale, Vector3f.UnitY, uv, Vector3f.UnitX));
			builder.AppendVertex(new Vertex(p3 + pos + hexNeighbors.BottomLeft * neighborScale, Vector3f.UnitY, uv, Vector3f.UnitX));
			builder.AppendVertex(new Vertex(p4 + pos + hexNeighbors.Left * neighborScale, Vector3f.UnitY, uv, Vector3f.UnitX));
			builder.AppendVertex(new Vertex(p5 + pos + hexNeighbors.TopLeft * neighborScale, Vector3f.UnitY, uv, Vector3f.UnitX));
		}

		// Creates 6 verticies
		public static void CreateHexVerticies(ref MeshBuilder<Vertex, uint> builder, in Vector3f pos, in Vector3f scale, in Vector2f uv)
		{
			Vector3f p0 = HEX.Span[0] * scale;
			Vector3f p1 = HEX.Span[1] * scale;
			Vector3f p2 = HEX.Span[2] * scale;
			Vector3f p3 = HEX.Span[3] * scale;
			Vector3f p4 = HEX.Span[4] * scale;
			Vector3f p5 = HEX.Span[5] * scale;

			builder.AppendVertex(new Vertex(p0 + pos, Vector3f.UnitY, uv, Vector3f.UnitX));
			builder.AppendVertex(new Vertex(p1 + pos, Vector3f.UnitY, uv, Vector3f.UnitX));
			builder.AppendVertex(new Vertex(p2 + pos, Vector3f.UnitY, uv, Vector3f.UnitX));
			builder.AppendVertex(new Vertex(p3 + pos, Vector3f.UnitY, uv, Vector3f.UnitX));
			builder.AppendVertex(new Vertex(p4 + pos, Vector3f.UnitY, uv, Vector3f.UnitX));
			builder.AppendVertex(new Vertex(p5 + pos, Vector3f.UnitY, uv, Vector3f.UnitX));
		}

		// Creates 12 verticies
		public static void CreateOuterHexVerticies(ref MeshBuilder<Vertex, uint> builder, in Vector3f pos, in Vector3f scale, in Vector3f bridgeScale, in Vector2f uv, float neighborScale, scoped Span<Vector3f> neighbors)
		{
			Vector3f p0 = HEX.Span[0] * scale;
			Vector3f p1 = HEX.Span[1] * scale;
			Vector3f p2 = HEX.Span[2] * scale;
			Vector3f p3 = HEX.Span[3] * scale;
			Vector3f p4 = HEX.Span[4] * scale;
			Vector3f p5 = HEX.Span[5] * scale;

			Vector3f right0 = Vector3f.Cross(Vector3f.Normalize(p1 - p0), Vector3f.UnitY) * INNER_RADIUS * bridgeScale;
			Vector3f right1 = Vector3f.Cross(Vector3f.Normalize(p2 - p1), Vector3f.UnitY) * INNER_RADIUS * bridgeScale;
			Vector3f right2 = Vector3f.Cross(Vector3f.Normalize(p3 - p2), Vector3f.UnitY) * INNER_RADIUS * bridgeScale;
			Vector3f right3 = Vector3f.Cross(Vector3f.Normalize(p4 - p3), Vector3f.UnitY) * INNER_RADIUS * bridgeScale;
			Vector3f right4 = Vector3f.Cross(Vector3f.Normalize(p5 - p4), Vector3f.UnitY) * INNER_RADIUS * bridgeScale;
			Vector3f right5 = Vector3f.Cross(Vector3f.Normalize(p0 - p5), Vector3f.UnitY) * INNER_RADIUS * bridgeScale;

			HexNeighbors<Vector3f> hexNeighbors = new(neighbors);

			builder.AppendVertex(new Vertex(p0 + right0 + pos + hexNeighbors.TopLeft * neighborScale, Vector3f.UnitY, uv, Vector3f.UnitX));
			builder.AppendVertex(new Vertex(p1 + right0 + pos + hexNeighbors.TopLeft * neighborScale, Vector3f.UnitY, uv, Vector3f.UnitX));
			builder.AppendVertex(new Vertex(p1 + right1 + pos + hexNeighbors.Left * neighborScale, Vector3f.UnitY, uv, Vector3f.UnitX));
			builder.AppendVertex(new Vertex(p2 + right1 + pos + hexNeighbors.Left * neighborScale, Vector3f.UnitY, uv, Vector3f.UnitX));
			builder.AppendVertex(new Vertex(p2 + right2 + pos + hexNeighbors.BottomLeft * neighborScale, Vector3f.UnitY, uv, Vector3f.UnitX));
			builder.AppendVertex(new Vertex(p3 + right2 + pos + hexNeighbors.BottomLeft * neighborScale, Vector3f.UnitY, uv, Vector3f.UnitX));
			builder.AppendVertex(new Vertex(p3 + right3 + pos + hexNeighbors.BottomRight * neighborScale, Vector3f.UnitY, uv, Vector3f.UnitX));
			builder.AppendVertex(new Vertex(p4 + right3 + pos + hexNeighbors.BottomRight * neighborScale, Vector3f.UnitY, uv, Vector3f.UnitX));
			builder.AppendVertex(new Vertex(p4 + right4 + pos + hexNeighbors.Right * neighborScale, Vector3f.UnitY, uv, Vector3f.UnitX));
			builder.AppendVertex(new Vertex(p5 + right4 + pos + hexNeighbors.Right * neighborScale, Vector3f.UnitY, uv, Vector3f.UnitX));
			builder.AppendVertex(new Vertex(p5 + right5 + pos + hexNeighbors.TopRight * neighborScale, Vector3f.UnitY, uv, Vector3f.UnitX));
			builder.AppendVertex(new Vertex(p0 + right5 + pos + hexNeighbors.TopRight * neighborScale, Vector3f.UnitY, uv, Vector3f.UnitX));
		}

		// Creates 12 verticies
		public static void CreateOuterHexVerticies(ref MeshBuilder<Vertex, uint> builder, in Vector3f pos, in Vector3f scale, in Vector3f bridgeScale, in Vector2f uv)
		{
			Vector3f p0 = HEX.Span[0] * scale;
			Vector3f p1 = HEX.Span[1] * scale;
			Vector3f p2 = HEX.Span[2] * scale;
			Vector3f p3 = HEX.Span[3] * scale;
			Vector3f p4 = HEX.Span[4] * scale;
			Vector3f p5 = HEX.Span[5] * scale;

			Vector3f right0 = Vector3f.Cross(Vector3f.Normalize(p1 - p0), Vector3f.UnitY) * INNER_RADIUS * bridgeScale;
			Vector3f right1 = Vector3f.Cross(Vector3f.Normalize(p2 - p1), Vector3f.UnitY) * INNER_RADIUS * bridgeScale;
			Vector3f right2 = Vector3f.Cross(Vector3f.Normalize(p3 - p2), Vector3f.UnitY) * INNER_RADIUS * bridgeScale;
			Vector3f right3 = Vector3f.Cross(Vector3f.Normalize(p4 - p3), Vector3f.UnitY) * INNER_RADIUS * bridgeScale;
			Vector3f right4 = Vector3f.Cross(Vector3f.Normalize(p5 - p4), Vector3f.UnitY) * INNER_RADIUS * bridgeScale;
			Vector3f right5 = Vector3f.Cross(Vector3f.Normalize(p0 - p5), Vector3f.UnitY) * INNER_RADIUS * bridgeScale;

			builder.AppendVertex(new Vertex(p0 + right0 + pos, Vector3f.UnitY, uv, Vector3f.UnitX));
			builder.AppendVertex(new Vertex(p1 + right0 + pos, Vector3f.UnitY, uv, Vector3f.UnitX));
			builder.AppendVertex(new Vertex(p1 + right1 + pos, Vector3f.UnitY, uv, Vector3f.UnitX));
			builder.AppendVertex(new Vertex(p2 + right1 + pos, Vector3f.UnitY, uv, Vector3f.UnitX));
			builder.AppendVertex(new Vertex(p2 + right2 + pos, Vector3f.UnitY, uv, Vector3f.UnitX));
			builder.AppendVertex(new Vertex(p3 + right2 + pos, Vector3f.UnitY, uv, Vector3f.UnitX));
			builder.AppendVertex(new Vertex(p3 + right3 + pos, Vector3f.UnitY, uv, Vector3f.UnitX));
			builder.AppendVertex(new Vertex(p4 + right3 + pos, Vector3f.UnitY, uv, Vector3f.UnitX));
			builder.AppendVertex(new Vertex(p4 + right4 + pos, Vector3f.UnitY, uv, Vector3f.UnitX));
			builder.AppendVertex(new Vertex(p5 + right4 + pos, Vector3f.UnitY, uv, Vector3f.UnitX));
			builder.AppendVertex(new Vertex(p5 + right5 + pos, Vector3f.UnitY, uv, Vector3f.UnitX));
			builder.AppendVertex(new Vertex(p0 + right5 + pos, Vector3f.UnitY, uv, Vector3f.UnitX));
		}

		public static void CreateHexIndicies(ref MeshBuilder<Vertex, uint> builder, uint offset)
		{
			builder.AppendTriange(offset, offset + 1, offset + 2);
			builder.AppendTriange(offset, offset + 2, offset + 3);
			builder.AppendTriange(offset, offset + 3, offset + 4);
			builder.AppendTriange(offset, offset + 4, offset + 5);
			builder.AppendTriange(offset, offset + 5, offset + 6);
			builder.AppendTriange(offset, offset + 6, offset + 1);
		}

		public static void CreateHexBridgeIndicies(ref MeshBuilder<Vertex, uint> builder, uint offset)
		{
			CreateHexBridge(ref builder, offset, 0);
			CreateHexBridge(ref builder, offset, 1);
			CreateHexBridge(ref builder, offset, 2);
			CreateHexBridge(ref builder, offset, 3);
			CreateHexBridge(ref builder, offset, 4);
			CreateHexBridge(ref builder, offset, 5);
		}

		public static void CreateHexBridgeCorners(ref MeshBuilder<Vertex, uint> builder, uint offset)
		{
			CreateHexBridgeCorner(ref builder, offset, 0);
			CreateHexBridgeCorner(ref builder, offset, 1);
			CreateHexBridgeCorner(ref builder, offset, 2);
			CreateHexBridgeCorner(ref builder, offset, 3);
			CreateHexBridgeCorner(ref builder, offset, 4);
			CreateHexBridgeCorner(ref builder, offset, 5);
		}

		static void CreateHexBridge(ref MeshBuilder<Vertex, uint> builder, uint offset, uint bridgeOffset)
		{
			builder.AppendTriange(offset + bridgeOffset * 2, offset + 12 + bridgeOffset * 2, offset + 12 + bridgeOffset * 2 + 1);
			builder.AppendTriange(offset + bridgeOffset * 2, offset + 12 + bridgeOffset * 2 + 1, offset + bridgeOffset * 2 + 1);
		}

		static void CreateHexBridgeCorner(ref MeshBuilder<Vertex, uint> builder, uint offset, uint bridgeOffset)
		{
			builder.AppendTriange(offset + bridgeOffset, offset + 6 + 12 * 2 + bridgeOffset, offset + 6 + 12 + bridgeOffset * 2);
			builder.AppendTriange(offset + bridgeOffset, offset + 6 + 12 + bridgeOffset * 2, offset + 6 + bridgeOffset * 2);

			builder.AppendTriange(offset + bridgeOffset, offset + 6 + 12 + (bridgeOffset * 2 - 1 + 12) % 12, offset + 6 + 12 * 2 + bridgeOffset);
			builder.AppendTriange(offset + bridgeOffset, offset + 6 + (bridgeOffset * 2 - 1 + 12) % 12, offset + 6 + 12 + (bridgeOffset * 2 - 1 + 12) % 12);
		}
	}

	class HexMap
	{
		const float OUTER_RADIUS = 1f;
		const float INNER_RADIUS = OUTER_RADIUS * 0.866025404f; // OUTER_RADIUS * Sqrt(3)

		const float solidFactor = 0.75f;
		const float blendFactor = 1 - solidFactor;

		Memory2D<HexCellData> map;

		public HexMap(int width, int height)
		{
			map = new HexCellData[width, height];
		}

		public Mesh Compile()
		{
			int bridgeSteps = 4;

			int vertexStride = 7 + (54 * bridgeSteps);
			int indexStride = 18 + (108 * bridgeSteps);

			using IMemoryOwner<Vertex> verticies = MemoryPool<Vertex>.Shared.Rent(map.Width * map.Height * vertexStride);
			using IMemoryOwner<uint> indicies = MemoryPool<uint>.Shared.Rent(map.Width * map.Height * indexStride);

			MeshBuilder<Vertex, uint> mapBuilder = new(verticies.Memory.Span, indicies.Memory.Span);

			Span<Vector3f> offsetsBridge = stackalloc Vector3f[6];
			Span<Vector3f> offsetsCorner = stackalloc Vector3f[6];

			Vector3f scaling = new Vector3f(INNER_RADIUS * 2, 1, OUTER_RADIUS * 1.5f) * 0.5f;
			for (int y = 0; y < map.Height; y++)
			{
				for (int x = 0; x < map.Width; x++)
				{
					// Creating a zig zag pattern, thus zagging the developer and making me look weak.
					Vector3f hexPos = new Vector3f(x + y / 2f - y / 2, 0, -y);
					hexPos *= scaling;

					float tileHeight = map.Span[x, y].height;
					Vector3f hexBaseOffset = Vector3f.UnitY * tileHeight;

					Vector3f hexScale = Vector3f.One * 0.5f;

					int heightIdx = GetHeightIdx(map.Span[x, y].height);
					Vector2f hexUv = Vector2f.Zero / 2f;
					hexUv += Vector2f.UnitX * 0.1f * heightIdx;

					using var n = GetNeighbors2(new HexCoord(x, y));
					ConstructHexMesh(ref mapBuilder, map.Span[x, y], n.Memory.Span, hexPos + hexBaseOffset, hexScale, hexUv, bridgeSteps);
				}
			}

			Mesh mapMesh = new Mesh();
			mapMesh.name = "Map";
			mapBuilder.WriteToMesh(ref mapMesh);
			mapMesh.RecalculateNormals();

			return mapMesh;
		}

		static void ConstructHexMesh(ref MeshBuilder<Vertex, uint> builder, in HexCellData cell, in Span<HexCellData> neighbors, in Vector3f hexPos, in Vector3f hexScale, in Vector2f hexUv, int bridgeSteps)
		{
			using var bridgeOffsets = GetBridgeOffsets(cell, neighbors);
			using var cornerOffsets = GetCornerOffsets(cell, neighbors);

			// Inner hex: 7 verticies
			int indexStart = builder.VertexOffset;
			HexMapBuilder.CreateCenterVertex(ref builder, hexPos, hexUv);
			HexMapBuilder.CreateHexVerticies(ref builder, hexPos, hexScale * solidFactor, hexUv, 0, bridgeOffsets.Memory.Span);
			HexMapBuilder.CreateHexIndicies(ref builder, (uint)indexStart);

			float step = blendFactor / bridgeSteps;
			for (int i = 0; i < bridgeSteps; i++)
			{
				float lerp = i / (float)bridgeSteps;
				float lerp1 = (i + 1) / (float)bridgeSteps;

				lerp = MathF.Round(lerp, 0);
				lerp1 = MathF.Round(lerp1, 0);

				// Bridge: 24 veritices
				indexStart = builder.VertexOffset;
				HexMapBuilder.CreateOuterHexVerticies(ref builder, hexPos, hexScale * solidFactor, hexScale * (step * i), hexUv, lerp, bridgeOffsets.Memory.Span);
				HexMapBuilder.CreateOuterHexVerticies(ref builder, hexPos, hexScale * solidFactor, hexScale * (step * (i + 1)), hexUv, lerp1, bridgeOffsets.Memory.Span);

				HexMapBuilder.CreateHexBridgeIndicies(ref builder, (uint)indexStart);

				// Bridge Corner: 30 verticies
				indexStart = builder.VertexOffset;
				HexMapBuilder.CreateHexVerticies(ref builder, hexPos, hexScale * (solidFactor + step * i), hexUv, lerp, cornerOffsets.Memory.Span);
				HexMapBuilder.CreateOuterHexVerticies(ref builder, hexPos, hexScale * solidFactor, hexScale * (step * i), hexUv, lerp, bridgeOffsets.Memory.Span);
				HexMapBuilder.CreateOuterHexVerticies(ref builder, hexPos, hexScale * solidFactor, hexScale * (step * (i + 1)), hexUv, lerp1, bridgeOffsets.Memory.Span);
				HexMapBuilder.CreateHexVerticies(ref builder, hexPos, hexScale * (solidFactor + step * (i + 1)), hexUv, lerp1, cornerOffsets.Memory.Span);

				HexMapBuilder.CreateHexBridgeCorners(ref builder, (uint)indexStart);
			}
		}

		static IMemoryOwner<Vector3f> GetBridgeOffsets(in HexCellData cell, in Span<HexCellData> neighbors)
		{
			float tileHeight = cell.height;

			var bridgeOffsets = MemoryPool<Vector3f>.Shared.Rent(6);
			var nNeighbors = new HexNeighbors<HexCellData>(neighbors);
			var oBridgeNeighbors = new HexNeighbors<Vector3f>(bridgeOffsets.Memory.Span);

			oBridgeNeighbors.TopRight = Vector3f.UnitY * (nNeighbors.TopRight.height - tileHeight) / 2;
			oBridgeNeighbors.Right = Vector3f.UnitY * (nNeighbors.Right.height - tileHeight) / 2;
			oBridgeNeighbors.BottomRight = Vector3f.UnitY * (nNeighbors.BottomRight.height - tileHeight) / 2;
			oBridgeNeighbors.BottomLeft = Vector3f.UnitY * (nNeighbors.BottomLeft.height - tileHeight) / 2;
			oBridgeNeighbors.Left = Vector3f.UnitY * (nNeighbors.Left.height - tileHeight) / 2;
			oBridgeNeighbors.TopLeft = Vector3f.UnitY * (nNeighbors.TopLeft.height - tileHeight) / 2;

			return bridgeOffsets;
		}

		static IMemoryOwner<Vector3f> GetCornerOffsets(in HexCellData cell, in Span<HexCellData> neighbors)
		{
			float tileHeight = cell.height;
			Vector3f hexBaseOffset = Vector3f.UnitY * tileHeight;

			var bridgeOffsets = MemoryPool<Vector3f>.Shared.Rent(6);
			var nNeighbors = new HexNeighbors<HexCellData>(neighbors);
			var oCornerNeighbors = new HexNeighbors<Vector3f>(bridgeOffsets.Memory.Span);

			oCornerNeighbors.TopRight = Vector3f.UnitY * (tileHeight + nNeighbors.TopRight.height + nNeighbors.TopLeft.height) / 3 - hexBaseOffset;
			oCornerNeighbors.Right = Vector3f.UnitY * (tileHeight + nNeighbors.TopLeft.height + nNeighbors.Left.height) / 3 - hexBaseOffset;
			oCornerNeighbors.BottomRight = Vector3f.UnitY * (tileHeight + nNeighbors.Left.height + nNeighbors.BottomLeft.height) / 3 - hexBaseOffset;
			oCornerNeighbors.BottomLeft = Vector3f.UnitY * (tileHeight + nNeighbors.BottomLeft.height + nNeighbors.BottomRight.height) / 3 - hexBaseOffset;
			oCornerNeighbors.Left = Vector3f.UnitY * (tileHeight + nNeighbors.BottomRight.height + nNeighbors.Right.height) / 3 - hexBaseOffset;
			oCornerNeighbors.TopLeft = Vector3f.UnitY * (tileHeight + nNeighbors.Right.height + nNeighbors.TopRight.height) / 3 - hexBaseOffset;

			return bridgeOffsets;
		}

		static int GetHeightIdx(float value)
		{
			if (value < 0.2f)
			{
				return 5;
			}
			else if (value < 0.4f)
			{
				return 0;
			}
			else if (value < 0.5f)
			{
				return 1;
			}
			else if (value < 0.7f)
			{
				return 2;
			}
			else if (value < 0.8f)
			{
				return 3;
			}
			else
			{
				return 4;
			}
		}

		public void Init()
		{
			FastNoiseLite noise = new FastNoiseLite(2345);
			noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);

			for (int y = 0; y < map.Height; y++)
			{
				for (int x = 0; x < map.Width; x++)
				{
					float n = noise.GetNoise(x * 10, y * 10);
					n += 1f;
					n /= 2;

					n = float.Clamp(n, 0, 1);
					n = MathF.Round(n, 2);

					map.Span[x, y] = new HexCellData(n);
				}
			}
		}

		public IMemoryOwner<HexCellData> GetNeighbors(HexCoord hex)
		{
			var axial = OddrToAxial(hex);

			var buff = MemoryPool<HexCellData>.Shared.Rent(6);
			buff.Memory.Span[0] = GetCell(axial + new HexAxialCoord(1, 0));
			buff.Memory.Span[1] = GetCell(axial + new HexAxialCoord(1, -1));
			buff.Memory.Span[2] = GetCell(axial + new HexAxialCoord(0, -1));
			buff.Memory.Span[3] = GetCell(axial + new HexAxialCoord(-1, 0));
			buff.Memory.Span[4] = GetCell(axial + new HexAxialCoord(-1, 1));
			buff.Memory.Span[5] = GetCell(axial + new HexAxialCoord(0, 1));

			return buff;
		}

		public IMemoryOwner<HexCellData> GetNeighbors2(HexCoord hex)
		{
			var axial = OddrToAxial(hex);

			var buff = MemoryPool<HexCellData>.Shared.Rent(6);
			buff.Memory.Span[0] = GetCell(axial + new HexAxialCoord(0, -1));
			buff.Memory.Span[1] = GetCell(axial + new HexAxialCoord(-1, 0));
			buff.Memory.Span[2] = GetCell(axial + new HexAxialCoord(-1, 1));
			buff.Memory.Span[3] = GetCell(axial + new HexAxialCoord(0, 1));
			buff.Memory.Span[4] = GetCell(axial + new HexAxialCoord(1, 0));
			buff.Memory.Span[5] = GetCell(axial + new HexAxialCoord(1, -1));

			return buff;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HexCellData GetCellWrap(in HexCoord hex)
		{
			int x = hex.X;
			int y = hex.Y;

			x = x < 0 ? x + map.Width : x;
			y = y < 0 ? y + map.Height : y;

			x = x >= map.Width ? x - map.Width : x;
			y = y >= map.Height ? y - map.Height : y;

			return map.Span[x, y];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HexCellData GetCell(in HexCoord hex)
		{
			int x = hex.X;
			int y = hex.Y;

			if (x < 0 || x >= map.Width || y < 0 || y >= map.Height)
				return new HexCellData(0);

			return map.Span[x, y];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HexCellData GetCell(in HexAxialCoord hex)
			=> GetCell(AxialToOddr(hex));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HexCellData GetCell(in HexCubeCoord hex)
			=> GetCell(CubeToAxial(hex));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		HexAxialCoord OddrToAxial(in HexCoord hex)
		{
			var q = hex.X - (hex.Y - (hex.Y & 1)) / 2;
			var r = hex.Y;

			return new HexAxialCoord(q, r);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		HexCoord AxialToOddr(in HexAxialCoord hex)
		{
			var col = hex.Q + (hex.R - (hex.R & 1)) / 2;
			var row = hex.R;
			return new HexCoord(col, row);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		HexCubeCoord AxialToCube(in HexAxialCoord hex)
		{
			var q = hex.Q;
			var r = hex.R;
			var s = -q - r;
			return new HexCubeCoord(q, r, s);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		HexAxialCoord CubeToAxial(in HexCubeCoord hex)
		{
			var q = hex.Q;
			var r = hex.R;
			return new HexAxialCoord(q, r);
		}
    }
}