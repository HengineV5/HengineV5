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
	}
}
