using EnCS.Attributes;
using Engine.Components;
using Engine.Graphics;
using Silk.NET.Input;
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

		[SystemUpdate]
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

	public static class QuaternionExtensions
	{
		public static Vector3 ToEulerAngles(this Quaternion q)
		{
			Vector3 euler = new Vector3();

			// roll (x-axis rotation)
			float sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
			float cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
			euler.X = MathF.Atan2(sinr_cosp, cosr_cosp);

			// pitch (y-axis rotation)
			float sinp = MathF.Sqrt(1 + 2 * (q.W * q.Y - q.X * q.Z));
			float cosp = MathF.Sqrt(1 - 2 * (q.W * q.Y - q.X * q.Z));
			euler.Y = 2 * MathF.Atan2(sinp, cosp) - MathF.PI / 2;

			// yaw (z-axis rotation)
			float siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
			float cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
			euler.Z = MathF.Atan2(siny_cosp, cosy_cosp);

			return euler;
		}
	}

	[System]
	public partial class MoveSystem
	{
		IInputHandler inputHandler;

		public MoveSystem(IInputHandler inputHandler)
		{
			this.inputHandler = inputHandler;
		}

		public void PreRun()
		{
			inputHandler.PollEvents();
		}

		Vector3 Multiply(Quaternion q, Vector3 v)
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

		[SystemUpdate]
		public void Update(Camera.Ref camera, Position.Ref position, Rotation.Ref rotation)
		{
			if (inputHandler.IsKeyDown(Key.ShiftLeft))
			{
				rotation.Set(UpdateRotation(rotation));
			}
			else
			{
				position.Set(UpdatePosition(position, rotation));
			}

			if (inputHandler.IsKeyDown(Key.Escape))
			{
				position.Set(Vector3.Zero);
				rotation.Set(Quaternion.Identity);
			}
		}

		public void PostRun()
		{
			
		}

		Vector3 UpdatePosition(Position.Ref position, Rotation.Ref rotation)
		{
			Quaternion camQ = new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w);
            Vector3 camForward = -Multiply(Quaternion.Inverse(camQ), Vector3.UnitZ);
			Vector3 camRight = Vector3.Cross(camForward, Vector3.UnitY);

			Vector3 delta = new Vector3();
			if (inputHandler.IsKeyDown(Key.W))
				delta += camForward * 0.1f;

			if (inputHandler.IsKeyDown(Key.A))
				delta -= camRight * 0.1f;

			if (inputHandler.IsKeyDown(Key.S))
				delta -= camForward * 0.1f;

			if (inputHandler.IsKeyDown(Key.D))
				delta += camRight * 0.1f;

			if (inputHandler.IsKeyDown(Key.Q))
				delta.Y += 0.1f;

			if (inputHandler.IsKeyDown(Key.E))
				delta.Y -= 0.1f;

			return delta + new Vector3(position.x, position.y, position.z);
		}

		Quaternion UpdateRotation(Rotation.Ref rotation)
		{
			Vector3 delta = new Vector3();
			/*
			if (inputHandler.IsKeyDown(Key.W))
				delta.Z -= 0.05f;
			*/

			if (inputHandler.IsKeyDown(Key.A))
				delta.X -= 0.05f;

			/*
			if (inputHandler.IsKeyDown(Key.S))
				delta.Z += 0.05f;
			*/

			if (inputHandler.IsKeyDown(Key.D))
				delta.X += 0.05f;

			if (inputHandler.IsKeyDown(Key.Q))
				delta.Y += 0.05f;

			if (inputHandler.IsKeyDown(Key.E))
				delta.Y -= 0.05f;

			var q = new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w);
			return Quaternion.Multiply(q, Quaternion.CreateFromYawPitchRoll(delta.X, delta.Y, delta.Z));
		}
	}
}
