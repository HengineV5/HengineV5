using Engine;
using Engine.Components;
using Engine.Graphics;
using Engine.Utils;
using static Engine.HengineEcs;

namespace Runner
{
	public static class WorldHelpers
	{
		public static EnCS.ArchRef<NEntity> CreateObject(this Main world, Vector3f pos, Vector3f scale, Mesh mesh, PbrMaterial material, int idx)
		{
			var objRef = world.Create(new NEntity.Vectorized());
			NEntity entRef = world.Get(objRef);
			entRef.Position.Set(pos);
			entRef.Rotation.Set(Quaternionf.Identity);
			entRef.Scale.Set(scale);
			entRef.Mesh.Set(mesh);
			entRef.PbrMaterial.Set(material);
			entRef.Networked.Set(new()
			{
				idx = idx
			});

			return objRef;
		}

		public static EnCS.ArchRef<Gizmo> CreateGizmo(this Main world, in Vector3f pos, in Vector3f scale, in GizmoType type, in GizmoColor color)
		{
			var objRef = world.Create(new Gizmo.Vectorized());
			Gizmo entRef = world.Get(objRef);
			entRef.Position.Set(pos);
			entRef.Rotation.Set(Quaternionf.Identity);
			entRef.Scale.Set(scale);
			entRef.GizmoComp.Set(new()
			{
				type = type,
				color = color
			});

			return objRef;
		}

		public static EnCS.ArchRef<HengineEcs.GizmoLine1> CreateGizmoLine(this Main world, in Vector3f p1, in Vector3f p2, in GizmoColor color)
		{
			var objRef = world.Create(new HengineEcs.GizmoLine1.Vectorized());
			HengineEcs.GizmoLine1 entRef = world.Get(objRef);
			entRef.GizmoLine.Set(new()
			{
				p1 = new(p1.x, p1.y, p1.z),
				p2 = new(p2.x, p2.y, p2.z),
				color = color
			});

			return objRef;
		}

		public static void CreateBezierCurve(this Main world, ref readonly Vector3f p1, ref readonly Vector3f p2, ref readonly Vector3f p3, int res)
		{
			var prev = p1;
			for (int a = 1; a <= res; a++)
			{
				var curr = Bezier.QuadraticBezierCurve(p1, p2, p3, a / (float)res);
				world.CreateGizmoLine(prev, curr, new GizmoColor(0, 1, 0));
				prev = curr;
			}
		}

		public static EnCS.ArchRef<Hex> CreateHex(this Main world, Vector3f pos, Vector3f scale, HexCell hexCell, Mesh mesh, PbrMaterial material, int idx)
		{
			var objRef = world.Create(new Hex.Vectorized());
			Hex entRef = world.Get(objRef);
			entRef.Position.Set(pos);
			entRef.Rotation.Set(Quaternionf.Identity);
			entRef.Scale.Set(scale);
			entRef.HexCell = hexCell;
			entRef.Mesh.Set(mesh);
			entRef.PbrMaterial.Set(material);
			entRef.Networked.Set(new()
			{
				idx = idx
			});

			return objRef;
		}

		public static EnCS.ArchRef<Cam> CreateCamera(this Main world, Camera.Comp camera, Vector3f position, in Skybox skybox, int idx)
		{
			var objRef = world.Create(new Cam.Vectorized());
			Cam entRef = world.Get(objRef);
			entRef.Camera.Set(camera);
			entRef.Position.Set(position);
			entRef.Rotation.Set(Quaternionf.Identity);
			entRef.Skybox.Set(skybox);
			entRef.Networked.Set(new()
			{
				idx = idx
			});

			return objRef;
		}

		public static void CreateCamera(this HengineServerEcs.Main world, Camera.Comp camera, Vector3f position, int idx)
		{
			var objRef = world.Create(new HengineServerEcs.Cam.Vectorized());
			HengineServerEcs.Cam entRef = world.Get(objRef);
			entRef.Camera.Set(camera);
			entRef.Position.Set(position);
			entRef.Rotation.Set(Quaternionf.Identity);
			entRef.Networked.Set(new()
			{
				idx = idx
			});
		}

		public static EnCS.ArchRef<HengineEcs.GuiButton1> CreateGuiButton(this HengineEcs.Overlay world, Vector4f position, Vector4f size, TextureAtlas textureAtlas, GuiButton.Comp guiButton, GuiProperties.Comp properties)
		{
			var objRef = world.Create(new HengineEcs.GuiButton1.Vectorized());
			HengineEcs.GuiButton1 entRef = world.Get(objRef);
			entRef.GuiPosition.Set(position);
			entRef.GuiSize.Set(size);
			entRef.TextureAtlas.Set(textureAtlas);
			entRef.GuiProperties.Set(properties);
			entRef.GuiButton.Set(guiButton);
			entRef.GuiState.Set(new());

			return objRef;
		}

		public static EnCS.ArchRef<TextElement> CreateTextElement(this HengineEcs.Overlay world, Vector4f position, TextureAtlas textureAtlas, GuiText text)
		{
			var objRef = world.Create(new TextElement.Vectorized());
			TextElement entRef = world.Get(objRef);
			entRef.GuiPosition.Set(position);
			entRef.TextureAtlas.Set(textureAtlas);
			entRef.GuiText.Set(text);

			return objRef;
		}
	}
}