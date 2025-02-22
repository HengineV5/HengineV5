
using EnCS.Attributes;
using Engine.Components;
using Engine.Graphics;
using Engine.Utils;
using Silk.NET.Input;
using Silk.NET.Windowing;
using System.Diagnostics;
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
	[SystemContext<EngineContext>]
	public partial class MoveSystem
	{
		IInputHandler inputHandler;
		IWindow window;

		bool mousePressed = false;

		Vector2f prevPos;
		Vector2f cameraRotation = Vector2f.Zero;

		public MoveSystem(IInputHandler inputHandler, IWindow window)
		{
			this.inputHandler = inputHandler;
			this.window = window;
		}

		public void Init()
		{
		}

		public void PreRun()
		{
			inputHandler.PollEvents();
		}

		[SystemUpdate]
		public void Update(ref EngineContext context, Camera.Ref camera, Position.Ref position, Rotation.Ref rotation)
		{
			window.Title = $"Hengine v5: {context.dt}";

			if (inputHandler.IsKeyDown(MouseButton.Right) && !mousePressed)
			{
				inputHandler.HideCursor();
				prevPos = inputHandler.GetMousePosition();

				mousePressed = true;
			}
			else if (!inputHandler.IsKeyDown(MouseButton.Right) && mousePressed)
			{
				inputHandler.ShowCursor();

				mousePressed = false;
			}

			position.Set(UpdatePosition_New(ref context, position, rotation));

			if (mousePressed)
			{
				rotation.Set(UpdateRotation_New(ref context, rotation));
			}

			if (inputHandler.IsKeyDown(Key.Escape))
			{
				position.Set(Vector3f.Zero);
				rotation.Set(Quaternionf.Identity);
			}
		}

		public void PostRun()
		{
			
		}

		Vector3f UpdatePosition_New(ref EngineContext context, Position.Ref position, Rotation.Ref rotation)
		{
			Quaternionf camQ = new Quaternionf(rotation.x, rotation.y, rotation.z, rotation.w);
            Vector3f camForward = -Vector3f.Transform(Vector3f.UnitZ, Quaternionf.Inverse(in camQ));
			Vector3f camRight = Vector3f.Normalize(Vector3f.Cross(camForward, Vector3f.UnitY));

            Vector3f delta = new();
			if (inputHandler.IsKeyDown(Key.W))
				delta += camForward * 5;

			if (inputHandler.IsKeyDown(Key.A))
				delta -= camRight * 5;

			if (inputHandler.IsKeyDown(Key.S))
				delta -= camForward * 5;

			if (inputHandler.IsKeyDown(Key.D))
				delta += camRight * 5;

			if (inputHandler.IsKeyDown(Key.Q))
				delta.y += 5;

			if (inputHandler.IsKeyDown(Key.E))
				delta.y -= 5;

			if (inputHandler.IsKeyDown(Key.ShiftLeft))
				delta *= 2;

			delta *= MathF.Max(context.dt, 0.0005f);
			return delta + new Vector3f(position.x, position.y, position.z);
		}

		Quaternionf UpdateRotation_New(ref EngineContext context, Rotation.Ref rotation)
		{
			Vector2f newPos = inputHandler.GetMousePosition();
			Vector2f mouseDelta = newPos - prevPos;
			prevPos = newPos;

			mouseDelta.y *= -1;
			mouseDelta *= 0.001f;
			cameraRotation += mouseDelta;

			return Quaternionf.FromAxisAngle(Vector3f.UnitX, -cameraRotation.y) * Quaternionf.FromAxisAngle(Vector3f.UnitY, cameraRotation.x);
		}
	}
}
