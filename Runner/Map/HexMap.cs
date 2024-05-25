using CommunityToolkit.HighPerformance;
using EnCS;
using Engine.Graphics;
using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using static Engine.HengineEcs;

namespace Runner
{
	class HexMap
	{
		const float OUTER_RADIUS = 1f;
		const float INNER_RADIUS = OUTER_RADIUS * 0.866025404f; // OUTER_RADIUS * Sqrt(3)

		const float solidFactor = 0.75f;
		const float blendFactor = 1 - solidFactor;

		Memory2D<HexCell> map;

		public HexMap(int width, int height)
		{
			map = new HexCell[width, height];
		}

		public Mesh Compile()
		{
			int vertexStride = 7 + 18 + 30;
			int indexStride = 3 * 6 + 3 * 12 * 2;

			Vertex[] verticies = new Vertex[map.Width * map.Height * vertexStride];
			uint[] indicies = new uint[map.Width * map.Height * indexStride];

			Span<Vector3> offsetsZero = stackalloc Vector3[6];
			Span<Vector3> offsets = stackalloc Vector3[6];
			Span<Vector3> offsets2 = stackalloc Vector3[6];

			Vector3 scaling = new Vector3(INNER_RADIUS * 2, 1, OUTER_RADIUS * 1.5f) * 0.5f;
			for (int y = 0; y < map.Height; y++)
			{
				for (int x = 0; x < map.Width; x++)
				{
					Vector3 p = new Vector3(x + y / 2f - y / 2, 0, -y);
					p *= scaling;

					Vector2 uvStart = new Vector2(0.05f, 0.05f);
					int heightIdx = 4;
					if (map.Span[x, y].height < 0.2f)
					{
						heightIdx = 5;
					}
					else if (map.Span[x, y].height < 0.4f)
					{
						heightIdx = 0;
					}
					else if (map.Span[x, y].height < 0.5f)
					{
						heightIdx = 1;
					}
					else if (map.Span[x, y].height < 0.7f)
					{
						heightIdx = 2;
					}
					else if (map.Span[x, y].height < 0.8f)
					{
						heightIdx = 3;
					}

					uvStart += Vector2.UnitX * 0.1f * heightIdx;

					Vector3 hexScale = Vector3.One * 0.5f;
					int hexIdx = x + y * map.Width;

					float tileHeight = map.Span[x, y].height;
					Vector3 hexOffset = Vector3.UnitY * tileHeight;
					//hexOffset *= 0.2f;

					p += hexOffset;

					using var n = GetNeighbors(new HexCoord(x, y));
					offsets[0] = Vector3.UnitY * (n.Memory.Span[1].height - tileHeight) / 2;
					offsets[1] = Vector3.UnitY * (n.Memory.Span[0].height - tileHeight) / 2;
					offsets[2] = Vector3.UnitY * (n.Memory.Span[5].height - tileHeight) / 2;
					offsets[3] = Vector3.UnitY * (n.Memory.Span[4].height - tileHeight) / 2;
					offsets[4] = Vector3.UnitY * (n.Memory.Span[3].height - tileHeight) / 2;
					offsets[5] = Vector3.UnitY * (n.Memory.Span[2].height - tileHeight) / 2;

					offsets2[0] = Vector3.UnitY * (tileHeight + n.Memory.Span[1].height + n.Memory.Span[2].height) / 3 - hexOffset;
					offsets2[1] = Vector3.UnitY * (tileHeight + n.Memory.Span[0].height + n.Memory.Span[1].height) / 3 - hexOffset;
					offsets2[2] = Vector3.UnitY * (tileHeight + n.Memory.Span[5].height + n.Memory.Span[0].height) / 3 - hexOffset;
					offsets2[3] = Vector3.UnitY * (tileHeight + n.Memory.Span[4].height + n.Memory.Span[5].height) / 3 - hexOffset;
					offsets2[4] = Vector3.UnitY * (tileHeight + n.Memory.Span[3].height + n.Memory.Span[4].height) / 3 - hexOffset;
					offsets2[5] = Vector3.UnitY * (tileHeight + n.Memory.Span[2].height + n.Memory.Span[3].height) / 3 - hexOffset;

					// Inner hex: 7 verticies
					MapWorld.CreateCenterVertex(verticies.AsSpan().Slice(vertexStride * hexIdx), p, uvStart);
					MapWorld.CreateHexVerticies(verticies.AsSpan().Slice(vertexStride * hexIdx + 1), p, hexScale * solidFactor, uvStart, offsetsZero);
					MapWorld.CreateHexIndicies(indicies.AsSpan().Slice(indexStride * hexIdx), (uint)(vertexStride * hexIdx));

					// Bridge: 32 veritices
					MapWorld.CreateOuterHexVerticies(verticies.AsSpan().Slice(vertexStride * hexIdx + 7), p, hexScale * solidFactor, hexScale * 0, uvStart, offsetsZero);
					MapWorld.CreateOuterHexVerticies(verticies.AsSpan().Slice(vertexStride * hexIdx + 7 + 12), p, hexScale * solidFactor, hexScale * blendFactor / 2, uvStart, offsets);
					MapWorld.CreateOuterHexVerticies(verticies.AsSpan().Slice(vertexStride * hexIdx + 7 + 12 * 2), p, hexScale * solidFactor, hexScale * blendFactor, uvStart, offsets);

					MapWorld.CreateHexBridgeIndicies(indicies.AsSpan().Slice(indexStride * hexIdx + 3 * 6), (uint)(vertexStride * hexIdx) + 7);
					MapWorld.CreateHexBridgeIndicies(indicies.AsSpan().Slice(indexStride * hexIdx + 3 * 6 + 6 * 6), (uint)(vertexStride * hexIdx) + 7 + 12);

					// Corners: 30 verticies
					/*
					MapWorld.CreateHexVerticies(verticies.AsSpan().Slice(vertexStride * hexIdx + 7 + 18), p, hexScale * solidFactor, uvStart, offsetsZero);
					MapWorld.CreateOuterHexVerticies(verticies.AsSpan().Slice(vertexStride * hexIdx + 7 + 18 + 6), p, hexScale * solidFactor, hexScale * blendFactor, uvStart, offsets);
					MapWorld.CreateHexVerticies(verticies.AsSpan().Slice(vertexStride * hexIdx + 7 + 18 + 6 + 12), p, hexScale, uvStart, offsets2);
					MapWorld.CreateHexBridgeCorners(indicies.AsSpan().Slice(indexStride * hexIdx + 3 * 6 + 3 * 6 * 2), (uint)(vertexStride * hexIdx) + 7 + 18);
					*/

				}
			}

			var mesh = new Mesh();
			mesh.name = "Map";
			mesh.verticies = verticies;
			mesh.indicies = indicies;

			mesh.RecalculateNormals();
            return mesh;
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

					if (n < 0 || n > 1)
						Console.WriteLine(n);

					n = float.Clamp(n, 0, 1);
					n = MathF.Round(n, 2);

					map.Span[x, y] = new HexCell(n);
				}
			}
		}

		public IMemoryOwner<HexCell> GetNeighbors(HexCoord hex)
		{
			var axial = OddrToAxial(hex);

			var buff = MemoryPool<HexCell>.Shared.Rent(6);
			buff.Memory.Span[0] = GetCell(axial + new HexAxialCoord(1, 0));
			buff.Memory.Span[1] = GetCell(axial + new HexAxialCoord(1, -1));
			buff.Memory.Span[2] = GetCell(axial + new HexAxialCoord(0, -1));
			buff.Memory.Span[3] = GetCell(axial + new HexAxialCoord(-1, 0));
			buff.Memory.Span[4] = GetCell(axial + new HexAxialCoord(-1, 1));
			buff.Memory.Span[5] = GetCell(axial + new HexAxialCoord(0, 1));

			return buff;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HexCell GetCell(in HexCoord hex)
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
		public HexCell GetCell(in HexAxialCoord hex)
			=> GetCell(AxialToOddr(hex));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HexCell GetCell(in HexCubeCoord hex)
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