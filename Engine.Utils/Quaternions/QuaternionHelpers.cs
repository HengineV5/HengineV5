using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
		public static Quaternion RotateOnto(Vector3 v1, Vector3 v2)
		{
			return Quaternion.Normalize(new Quaternion(Vector3.Cross(v1, v2), MathF.Sqrt(v1.LengthSquared() * v2.LengthSquared()) + Vector3.Dot(v1, v2)));
		}

		/// <summary>
		/// Rotate a vector by a quaternion
		/// </summary>
		/// <param name="q"></param>
		/// <param name="v"></param>
		/// <returns></returns>
		public static Vector3 Multiply(Quaternion q, Vector3 v)
		{
			// Extract the vector part of the quaternion
			Vector3 u = new(q.X, q.Y, q.Z);

			// Extract the scalar part of the quaternion
			float s = q.W;

			// Do the math
			return 2.0f * Vector3.Dot(u, v) * u
					+ (s * s - Vector3.Dot(u, u)) * v
					+ 2.0f * s * Vector3.Cross(u, v);
		}
	}
}
