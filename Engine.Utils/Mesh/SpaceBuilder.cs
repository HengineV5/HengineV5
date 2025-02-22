using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Utils.Mesh
{
	public struct ShapeId
	{

	}

	public struct SurfaceId
	{

	}

	public struct MeshId
	{

	}

	public struct PlaneId
	{
		internal int plane;

		public PlaneId(int plane)
		{
			this.plane = plane;
		}
	}

	public ref struct IdTuple<T1, T2>
		where T1 : struct
		where T2 : struct
	{
		public ref readonly T1 ref1;
		public ref readonly T2 ref2;

		public IdTuple(ref readonly T1 ref1, ref readonly T2 ref2)
		{
			this.ref1 = ref ref1;
			this.ref2 = ref ref2;
		}
	}

	struct PlaneInfo
	{
		public Vector3f position;
		public Vector3f up;

		public PlaneInfo(Vector3f position, Vector3f up)
		{
			this.position = position;
			this.up = up;
		}
	}

	struct ShapeInfo
	{
		public int plane;
		public Memory<Vector2> outline;
	}

	public class MeshBuilder
	{
		internal List<PlaneInfo> planes = new();
		internal List<ShapeInfo> shapes = new();

		MeshId mid = new();
		PlaneId pid = new();
		ShapeId sid = new();
		SurfaceId SurfaceId = new();

        private MeshBuilder()
        {
            
        }

		internal ref MeshId GetMeshId()
		{
			return ref mid;
		}

		internal ref PlaneId GetPlaneId()
		{
			return ref pid;
		}

		internal ref ShapeId GetShapeId()
		{
			return ref sid;
		}

		internal ref SurfaceId GetSurfaceId()
		{
			return ref SurfaceId;
		}

        public static void Build(Action<SpaceBuilder> meshBuilder, out Memory<Vector3f> verticies, out Memory<int> indicies)
		{
			var builder = new MeshBuilder();

			meshBuilder(new(builder));

			verticies = new Vector3f[0];
			indicies = new int[0];
		}
	}

	public ref struct SpaceBuilder
	{
		MeshBuilder builder;

		public SpaceBuilder(MeshBuilder builder)
		{
			this.builder = builder;
		}

		public ref readonly PlaneId Plane(Vector3f position, Vector3f up, Action<PlaneBuilder> planeBuilder)
		{
			builder.planes.Add(new()
			{
				position = position,
				up = up,
			});

			planeBuilder(new(builder, builder.planes.Count - 1));

			return ref builder.GetPlaneId();
		}
	}

	public ref struct PlaneBuilder
	{
		MeshBuilder builder;
		int plane;

		public PlaneBuilder(MeshBuilder builder, int plane)
		{
			this.builder = builder;
			this.plane = plane;
		}

		public ref readonly ShapeId Square(Vector2 position, Vector2 size)
		{
			Memory<Vector2> outline = new Vector2[4];
			outline.Span[0] = position;
			outline.Span[1] = position + new Vector2(size.Y);
			outline.Span[2] = position + size;
			outline.Span[3] = position + new Vector2(size.X);

			builder.shapes.Add(new()
			{
				outline = outline,
				plane = plane,
			});
			return ref builder.GetShapeId();
		}

		public ref readonly ShapeId Circle(Vector2 position, float radius)
		{
			return ref builder.GetShapeId();
		}

		public ref readonly ShapeId Hex(Vector2 position, float radius)
		{
			return ref builder.GetShapeId();
		}

		public ref readonly SurfaceId Triangulate(ref readonly ShapeId shape)
		{
			return ref builder.GetSurfaceId();
		}

		public IdTuple<SurfaceId, SurfaceId> Split(ref readonly SurfaceId surface, scoped ReadOnlySpan<Vector2> seam)
		{
			return new(ref builder.GetSurfaceId(), ref builder.GetSurfaceId());
		}

		public IdTuple<SurfaceId, SurfaceId> Split(scoped ReadOnlySpan<SurfaceId> surface, scoped ReadOnlySpan<Vector2> seam)
		{
			return new(ref builder.GetSurfaceId(), ref builder.GetSurfaceId());
		}
	}
}
