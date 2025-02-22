using Engine;
using Engine.Components;
using Engine.Graphics;
using static Engine.HengineEcs;

namespace Runner
{
	public static class WorldHelpers
	{
		public static EnCS.ArchRef<NEntity> CreateObject(this Main world, Vector3f pos, Vector3f scale, Mesh mesh, PbrMaterial material, int idx)
		{
			var objRef = world.Create(new NEntity());
			NEntity.Ref entRef = world.Get(objRef);
			entRef.Position.Set(pos);
			entRef.Rotation.Set(Quaternionf.Identity);
			entRef.Scale.Set(scale);
			entRef.Mesh.Set(mesh);
			entRef.PbrMaterial.Set(material);
			entRef.Networked.Set(new Networked()
			{
				idx = idx
			});

			return objRef;
		}

		public static EnCS.ArchRef<Gizmo> CreateGizmo(this Main world, in Vector3f pos, in Vector3f scale, in GizmoType type, in GizmoColor color)
		{
			var objRef = world.Create(new Gizmo());
			Gizmo.Ref entRef = world.Get(objRef);
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

		public static EnCS.ArchRef<HengineEcs.GizmoLine> CreateGizmoLine(this Main world, in Vector3f p1, in Vector3f p2, in GizmoColor color)
		{
			var objRef = world.Create(new HengineEcs.GizmoLine());
			HengineEcs.GizmoLine.Ref entRef = world.Get(objRef);
			entRef.GizmoLine.Set(new()
			{
				p1 = new(p1.x, p1.y, p1.z),
				p2 = new(p2.x, p2.y, p2.z),
				color = color
			});

			return objRef;
		}

		public static EnCS.ArchRef<Hex> CreateHex(this Main world, Vector3f pos, Vector3f scale, HexCell hexCell, Mesh mesh, PbrMaterial material, int idx)
		{
			var objRef = world.Create(new Hex());
			Hex.Ref entRef = world.Get(objRef);
			entRef.Position.Set(pos);
			entRef.Rotation.Set(Quaternionf.Identity);
			entRef.Scale.Set(scale);
			entRef.HexCell.Set(hexCell);
			entRef.Mesh.Set(mesh);
			entRef.PbrMaterial.Set(material);
			entRef.Networked.Set(new Networked()
			{
				idx = idx
			});

			return objRef;
		}

		public static EnCS.ArchRef<Cam> CreateCamera(this Main world, Camera camera, Vector3f position, in Skybox skybox, int idx)
		{
			var objRef = world.Create(new Cam());
			Cam.Ref entRef = world.Get(objRef);
			entRef.Camera.Set(camera);
			entRef.Position.Set(position);
			entRef.Rotation.Set(Quaternionf.Identity);
			entRef.Skybox.Set(skybox);
			entRef.Networked.Set(new Networked()
			{
				idx = idx
			});

			return objRef;
		}

		public static void CreateCamera(this HengineServerEcs.Main world, Camera camera, Vector3f position, int idx)
		{
			var objRef = world.Create(new HengineServerEcs.Cam());
			HengineServerEcs.Cam.Ref entRef = world.Get(objRef);
			entRef.Camera.Set(camera);
			entRef.Position.Set(position);
			entRef.Rotation.Set(Quaternionf.Identity);
			entRef.Networked.Set(new Networked()
			{
				idx = idx
			});
		}

		public static EnCS.ArchRef<GuiElement> CreateGuiElement(this HengineEcs.Overlay world, Vector4f position, Vector4f size, TextureAtlas textureAtlas, GuiProperties properties)
		{
			var objRef = world.Create(new GuiElement());
			GuiElement.Ref entRef = world.Get(objRef);
			entRef.GuiPosition.Set(position);
			entRef.GuiSize.Set(size);
			entRef.TextureAtlas.Set(textureAtlas);
			entRef.GuiProperties.Set(properties);

			return objRef;
		}

		public static EnCS.ArchRef<TextElement> CreateTextElement(this HengineEcs.Overlay world, Vector4f position, TextureAtlas textureAtlas, GuiText text)
		{
			var objRef = world.Create(new TextElement());
			TextElement.Ref entRef = world.Get(objRef);
			entRef.GuiPosition.Set(position);
			entRef.TextureAtlas.Set(textureAtlas);
			entRef.GuiText.Set(text);

			return objRef;
		}
	}
}