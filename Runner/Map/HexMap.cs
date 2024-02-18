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

		Memory2D<ArchRef<Hex>> map;

		public HexMap(int width, int height)
		{
			map = new ArchRef<Hex>[width, height];
		}

		public void Init(Main world, ref Mesh meshSphere, ref Mesh meshMap, ref PbrMaterial materialSphere)
		{
			FastNoiseLite noise = new FastNoiseLite(2345);
			noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);

			var axisMatX = MapWorld.GetPointMaterial(new Vector3(1, 0.01f, 0.01f));
			var axisMatY = MapWorld.GetPointMaterial(new Vector3(0.01f, 0.01f, 1));
			var axisMatZ = MapWorld.GetPointMaterial(new Vector3(0.01f, 1, 0.01f));
			var distMat = MapWorld.GetPointMaterial(new Vector3(0.01f, 1, 1));

			HexCoord point = new HexCoord(5, 5);

			Memory<PbrMaterial> materials = new PbrMaterial[10];
			for (int i = 0; i < 10; i++)
			{
				materials.Span[i] = MapWorld.GetPointMaterial(Vector3.One * 0.1f * i);
			}

			materials.Span[0] = MapWorld.GetPointMaterial(new Vector3(0, 0.1f, 1f));
			materials.Span[1] = MapWorld.GetPointMaterial(new Vector3(0, 0.1f, 1f));
			materials.Span[2] = MapWorld.GetPointMaterial(new Vector3(0, 0.1f, 1f));
			materials.Span[3] = MapWorld.GetPointMaterial(new Vector3(0, 0.1f, 1f));
			materials.Span[4] = MapWorld.GetPointMaterial(new Vector3(0.96f, 0.85f, 0.21f));
			materials.Span[5] = MapWorld.GetPointMaterial(new Vector3(0.31f, 0.61f, 0.13f));
			materials.Span[6] = MapWorld.GetPointMaterial(new Vector3(0.31f, 0.61f, 0.13f));

			Vector3 start = new(-(map.Width / 2) + 0.5f, -2, -5);
			Vector3 scaling = new Vector3(INNER_RADIUS * 2, 1, OUTER_RADIUS * 1.5f) * 0.5f;
			for (int y = 0; y < map.Height; y++)
			{
				for (int x = 0; x < map.Width; x++)
				{
					Vector3 p = new Vector3(x + y / 2f - y / 2, 0, -y);
					p *= scaling;
					p += start;

					/*
                    var axial = OddrToAxial(new HexCoord(x, y));
					var cube = AxialToCube(axial);
					var cubePoint = AxialToCube(OddrToAxial(point));

                    world.CreateObject(p, Vector3.One * 0.1f, meshSphere, materialSphere, 1);

					ArchRef<NEntity> entRef;

					if (cube.Q == cubePoint.Q)
						entRef = world.CreateObject(p, Vector3.One * 0.5f, meshMap, axisMatX, 1);
					else if (cube.R == cubePoint.R)
						entRef = world.CreateObject(p, Vector3.One * 0.5f, meshMap, axisMatY, 1);
					else if (cube.S == cubePoint.S)
						entRef = world.CreateObject(p, Vector3.One * 0.5f, meshMap, axisMatZ, 1);
					else if (HexCubeCoord.Distance(cube, cubePoint) <= 3)
						entRef = world.CreateObject(p, Vector3.One * 0.5f, meshMap, distMat, 1);
					else
						entRef = world.CreateObject(p, Vector3.One * 0.5f, meshMap, MapWorld.GetPointMaterial(MapWorld.RandomColor()), 1);
					map.Span[x, y] = entRef;
					*/

					float n = noise.GetNoise(x * 10, y * 10);
					n += 1f;
					n /= 2;

					if (n < 0 || n > 1)
						Console.WriteLine(n);

					n = float.Clamp(n, 0, 1);

                    map.Span[x, y] = world.CreateHex(p, Vector3.One * 0.5f, new HexCell(n), meshMap, materials.Span[(int)MathF.Round(n * 9)], 1);
				}
			}
		}

		public IMemoryOwner<ArchRef<Hex>> GetNeighbors(HexCoord hex)
		{
			var axial = OddrToAxial(hex);

			var buff = MemoryPool<ArchRef<Hex>>.Shared.Rent(6);
			buff.Memory.Span[0] = GetCell(axial + new HexAxialCoord(1, 0));
			buff.Memory.Span[1] = GetCell(axial + new HexAxialCoord(1, -1));
			buff.Memory.Span[2] = GetCell(axial + new HexAxialCoord(0, -1));
			buff.Memory.Span[3] = GetCell(axial + new HexAxialCoord(-1, 0));
			buff.Memory.Span[4] = GetCell(axial + new HexAxialCoord(-1, 1));
			buff.Memory.Span[5] = GetCell(axial + new HexAxialCoord(0, 1));

			return buff;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ArchRef<Hex> GetCell(in HexCoord hex)
		{
			return map.Span[hex.X, hex.Y];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ArchRef<Hex> GetCell(in HexAxialCoord hex)
			=> GetCell(AxialToOddr(hex));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ArchRef<Hex> GetCell(in HexCubeCoord hex)
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