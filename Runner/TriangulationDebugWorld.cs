using EnCS;
using Engine;
using Engine.Components;
using Engine.Utils;
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
			int end = 208;

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
			p.Add(new(510f, -31f));
			p.Add(new(290.5f, 29.5f));
			p.Add(new(144.5f, 170f));
			p.Add(new(73f, 407f));
			p.Add(new(254f, 441f));
			p.Add(new(275f, 296f));
			p.Add(new(328f, 228f));
			p.Add(new(404f, 132f));
			p.Add(new(510f, 121f));
			p.Add(new(510f, 694f));
			p.Add(new(721f, 134f));
			p.Add(new(789.5f, 212f));
			p.Add(new(858f, 290f));
			p.Add(new(858f, 405f));
			p.Add(new(858f, 503f));
			p.Add(new(809.5f, 562.5f));
			p.Add(new(761f, 622f));
			p.Add(new(616f, 669f));
			p.Add(new(616f, 121f));
			p.Add(new(721f, 134f));
			p.Add(new(510f, 694f));
			p.Add(new(283f, 780f));
			p.Add(new(150.5f, 913f));
			p.Add(new(104f, 1106f));
			p.Add(new(240f, 1417f));
			p.Add(new(510f, 1515f));
			p.Add(new(510f, 1601f));
			p.Add(new(616f, 1601f));
			p.Add(new(616f, 1515f));
			p.Add(new(865f, 1423f));
			p.Add(new(1007f, 1154f));
			p.Add(new(821f, 1126f));
			p.Add(new(805f, 1232f));
			p.Add(new(754.5f, 1288.5f));
			p.Add(new(704f, 1345f));
			p.Add(new(616f, 1363f));
			p.Add(new(616f, 844f));
			p.Add(new(405f, 1349f));
			p.Add(new(344.5f, 1281f));
			p.Add(new(284f, 1213f));
			p.Add(new(284f, 1120f));
			p.Add(new(284f, 1028f));
			p.Add(new(335.5f, 966f));
			p.Add(new(387f, 904f));
			p.Add(new(510f, 867f));
			p.Add(new(510f, 1365f));
			p.Add(new(405f, 1349f));
			p.Add(new(616f, 844f));
			p.Add(new(796f, 791f));
			p.Add(new(933f, 701f));
			p.Add(new(1014.5f, 575f));
			p.Add(new(1043f, 417f));
			p.Add(new(924f, 105f));
			p.Add(new(616f, -29f));
			p.Add(new(616f, -211f));
			p.Add(new(510f, -211f));
		}
	}
}
