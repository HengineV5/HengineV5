using EnCS;
using Engine;
using Engine.Components;
using Engine.Utils;
using Engine.Utils.Parsing.TTF;
using Silk.NET.Input;
using System.Runtime.InteropServices;
using UtilLib.Span;
using static Engine.HengineEcs;

namespace Runner
{
	internal class TriangulationDebugWorld
	{
		static int steps = 0;
		static int vert = 0;
		static bool isDown = false;
		static bool isDown2 = false;

		static List<ArchRef<Gizmo>> pointRefs = new();
		static List<ArchRef<GizmoLine1>> lineRefs = new();

		static void InputHandler(HengineEcs ecs, IInputHandler inputHandler)
		{
			int end = 28;

			while (true)
			{
				if (!isDown && inputHandler.IsKeyDown(Key.Right))
				{
					isDown = true;
					steps++;
					vert = 0;

					Console.WriteLine($"Steps: {steps}");
					ShowMesh(ecs.GetMain());
				}
				else if (!isDown && steps > 0 && inputHandler.IsKeyDown(Key.Left))
				{
					isDown = true;
					steps--;
					vert = 0;

					Console.WriteLine($"Steps: {steps}");
					ShowMesh(ecs.GetMain());
				}
				else if (!isDown && inputHandler.IsKeyDown(Key.Delete))
				{
					isDown = true;
					steps = end - 1;
					vert = 0;

					Console.WriteLine($"Steps: {steps}");
					ShowMesh(ecs.GetMain());
				}
				else if (!inputHandler.IsKeyDown(Key.Left) && !inputHandler.IsKeyDown(Key.Right))
				{
					isDown = false;
				}

				if (!isDown2 && inputHandler.IsKeyDown(Key.Up))
				{
					isDown2 = true;
					vert++;

					ShowMesh(ecs.GetMain());
					Console.WriteLine($"Vert: {vert}");
				}
				else if (!isDown2 && inputHandler.IsKeyDown(Key.Down))
				{
					isDown2 = true;
					vert--;

					ShowMesh(ecs.GetMain());
					Console.WriteLine($"Vert: {vert}");
				}
				else if (!inputHandler.IsKeyDown(Key.Up) && !inputHandler.IsKeyDown(Key.Down))
				{
					isDown2 = false;
				}

				Thread.Sleep(10);
			}
		}

		static void ShowMesh(Main world)
		{
			for (int i = pointRefs.Count - 1; i >= 0; i--)
			{
				world.Delete(pointRefs[i]);
			}

			for (int i = lineRefs.Count - 1; i >= 0; i--)
			{
				world.Delete(lineRefs[i]);
			}

			pointRefs.Clear();
			lineRefs.Clear();

			List<Vector2f> points = new();
			GetMesh(points);

			scoped Span<Vector2f> pointsSpan = CollectionsMarshal.AsSpan(points);
			Memory<int> indices = Triangulation.TriangulateStep(pointsSpan, steps, margin: 0.0001f);

			//Console.WriteLine($"To Remove:");
			for (int i = 0; i < indices.Length / 3; i++)
			{
				//Console.WriteLine($"\t{indices.Span[i * 3 + 1]}");

				//points.RemoveAt(indices.Span[i * 3 + 1]);
				points[indices.Span[i * 3 + 1]] = new(-1.3f, -1.2f);
			}

			points.RemoveAll(points => points.x == -1.3f && points.y == -1.2f);

			pointsSpan = CollectionsMarshal.AsSpan(points);
			SpanRingBuffer<Vector2f> ringBuff = pointsSpan;

			float scale = 0.01f;
			float pointScale = 0.5f;

			for (int i = 0; i < ringBuff.Length; i++)
			{
				var curr = new Vector3f(ringBuff[i].x, 0, -ringBuff[i].y) * scale;
				var next = new Vector3f(ringBuff[i + 1].x, 0, -ringBuff[i + 1].y) * scale;

				pointRefs.Add(world.CreateGizmo(curr, Vector3f.One * pointScale * ((i == vert - 1 || i == vert || i == vert + 1) ? 1.1f : 1.0f), GizmoType.Point, (i == vert - 1 || i == vert || i == vert + 1) ? new(0, 1, 0) : new(1, 0, 0)));
				lineRefs.Add(world.CreateGizmoLine(curr, next, vert == i ? new(0, 1, 0) : new(0, 0, 0)));

				if (i == vert - 1 || i == vert || i == vert + 1)
				{
					curr += Vector3f.UnitY;
					pointRefs.Add(world.CreateGizmo(curr, Vector3f.One * pointScale * 0.5f, GizmoType.Point, new(0, 1, 1)));
				}
			}
		}

		public static void Load(HengineEcs ecs, IInputHandler inputHandler)
		{
			new Thread(() => InputHandler(ecs, inputHandler)).Start();
			Main world = ecs.GetMain();
			ShowMesh(world);
		}

		static void GetMesh(List<Vector2f> p)
		{
            var font = TtfLoader.LoadFont("Fonts/arial.ttf");
            var b = font.Glyphs[font.GetUnicodeGlyphIndex('7')];
			for (int i = 0; i < b.coords.Length; i++)
			{
				p.Add(new(b.coords.Span[i].x, b.coords.Span[i].y));
            }
        }
	}
}
