using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Utils
{
	public static class QuaternionHelpers
	{
		/// <summary>
		/// Rotate vector v2 onto vector v2
		/// </summary>
		/// <returns></returns>
		public static Quaternionf RotateOnto(Vector3f v1, Vector3f v2)
		{
			var v = Vector3f.Cross(in v1, in v2);
			return Quaternionf.Normalize(new Quaternionf(v.x, v.y, v.z, MathF.Sqrt(Vector3f.LengthSquared(in v1) * Vector3f.LengthSquared(in v2)) + Vector3f.Dot(v1, v2)));
		}

		/// <summary>
		/// Rotate a vector by a quaternion
		/// </summary>
		/// <param name="q"></param>
		/// <param name="v"></param>
		/// <returns></returns>
		public static Vector3f Multiply(Quaternionf q, Vector3f v)
		{
			// Extract the vector part of the quaternion
			Vector3f u = new(q.x, q.y, q.z);

			// Extract the scalar part of the quaternion
			float s = q.w;

			// Do the math
			return 2.0f * Vector3f.Dot(in u, in v) * u
					+ (s * s - Vector3f.Dot(in u, in u)) * v
					+ 2.0f * s * Vector3f.Cross(in u, in v);
		}
	}
}
