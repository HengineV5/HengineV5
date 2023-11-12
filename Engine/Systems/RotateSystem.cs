using EnCS.Attributes;
using Engine.Components;
using System.Numerics;

namespace Engine
{
	[System]
	public partial class RotateSystem
	{
		public void Init()
		{

		}

		public void Dispose()
		{

		}

		public void PreRun()
		{

		}

		public void Update(Rotation.Ref rotation)
		{
			var randX = (Random.Shared.NextSingle() - 0.5f) / 100;
			var randY = (Random.Shared.NextSingle() - 0.5f) / 100;
			var randZ = (Random.Shared.NextSingle() - 0.5f) / 100;

			Quaternion q = new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w);
			q *= Quaternion.CreateFromYawPitchRoll(randX, randY, randZ);

            rotation.x = q.X;
			rotation.y = q.Y;
			rotation.z = q.Z;
			rotation.w = q.W;
		}

		public void PostRun()
		{

		}
	}
}
