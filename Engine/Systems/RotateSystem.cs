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

		Vector2 prevPos;
		//Vector2 cameraRotation = new Vector2(-MathF.PI / 2f, 0);
		Vector2 cameraRotation = Vector2.Zero;

		public MoveSystem(IInputHandler inputHandler, IWindow window)
		{
			this.inputHandler = inputHandler;
			this.window = window;
		}

		public void Init()
		{
			//inputHandler.HideCursor();
			//prevPos = inputHandler.GetMousePosition();
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

			position.Set(UpdatePosition(ref context, position, rotation));

			if (mousePressed)
				rotation.Set(UpdateRotation(ref context, rotation));

			if (inputHandler.IsKeyDown(Key.Escape))
			{
				position.Set(Vector3.Zero);
				rotation.Set(Quaternion.Identity);
			}
		}

		public void PostRun()
		{
			
		}

		Vector3 UpdatePosition(ref EngineContext context, Position.Ref position, Rotation.Ref rotation)
		{
			Quaternion camQ = new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w);
            Vector3 camForward = -QuaternionHelpers.Multiply(Quaternion.Inverse(camQ), Vector3.UnitZ);
			Vector3 camRight = Vector3.Normalize(Vector3.Cross(camForward, Vector3.UnitY));

            Vector3 delta = new Vector3();
			if (inputHandler.IsKeyDown(Key.W))
				delta += camForward * 5;

			if (inputHandler.IsKeyDown(Key.A))
				delta -= camRight * 5;

			if (inputHandler.IsKeyDown(Key.S))
				delta -= camForward * 5;

			if (inputHandler.IsKeyDown(Key.D))
				delta += camRight * 5;

			if (inputHandler.IsKeyDown(Key.Q))
				delta.Y += 5;

			if (inputHandler.IsKeyDown(Key.E))
				delta.Y -= 5;

			if (inputHandler.IsKeyDown(Key.ShiftLeft))
				delta *= 2;

			delta *= MathF.Max(context.dt, 0.0005f);
			return delta + new Vector3(position.x, position.y, position.z);
		}

		Quaternion UpdateRotation(ref EngineContext context, Rotation.Ref rotation)
		{
			Vector2 newPos = inputHandler.GetMousePosition();
			Vector2 mouseDelta = newPos - prevPos;
			prevPos = newPos;

			mouseDelta.Y *= -1;
			mouseDelta *= 0.001f;
			cameraRotation += mouseDelta;

            Vector3 cameraDir = new Vector3();
			cameraDir.X = MathF.Cos(cameraRotation.X) * MathF.Cos(cameraRotation.Y);
			cameraDir.Y = MathF.Sin(cameraRotation.Y);
			cameraDir.Z = MathF.Sin(cameraRotation.X) * MathF.Cos(cameraRotation.Y);

			return Quaternion.CreateFromRotationMatrix(Matrix4x4.CreateLookAt(Vector3.Zero, cameraDir, Vector3.UnitY));
		}
	}
}
