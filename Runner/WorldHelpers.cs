using Engine;
using Engine.Components;
using Engine.Graphics;
using System.Numerics;
using static Engine.HengineEcs;

namespace Runner
{
	public static class WorldHelpers
	{
		public static EnCS.ArchRef<NEntity> CreateObject(this Main world, Vector3 pos, Vector3 scale, Mesh mesh, PbrMaterial material, int idx)
		{
			var objRef = world.Create(new NEntity());
			NEntity.Ref entRef = world.Get(objRef);
			entRef.Position.Set(pos);
			entRef.Rotation.Set(Quaternion.Identity);
			entRef.Scale.Set(scale);
			entRef.Mesh.Set(mesh);
			entRef.PbrMaterial.Set(material);
			entRef.Networked.Set(new Networked()
			{
				idx = idx
			});

			return objRef;
		}

		public static EnCS.ArchRef<Gizmo> CreateGizmo(this Main world, Vector3 pos, Vector3 scale, GizmoComp gizmo)
		{
			var objRef = world.Create(new Gizmo());
			Gizmo.Ref entRef = world.Get(objRef);
			entRef.Position.Set(pos);
			entRef.Rotation.Set(Quaternion.Identity);
			entRef.Scale.Set(scale);
			entRef.GizmoComp.Set(gizmo);

			return objRef;
		}

		public static EnCS.ArchRef<Hex> CreateHex(this Main world, Vector3 pos, Vector3 scale, HexCell hexCell, Mesh mesh, PbrMaterial material, int idx)
		{
			var objRef = world.Create(new Hex());
			Hex.Ref entRef = world.Get(objRef);
			entRef.Position.Set(pos);
			entRef.Rotation.Set(Quaternion.Identity);
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

		public static EnCS.ArchRef<Cam> CreateCamera(this Main world, Camera camera, Vector3 position, in Skybox skybox, int idx)
		{
			var objRef = world.Create(new Cam());
			Cam.Ref entRef = world.Get(objRef);
			entRef.Camera.Set(camera);
			entRef.Position.Set(position);
			entRef.Rotation.Set(Quaternion.Identity);
			entRef.Skybox.Set(skybox);
			entRef.Networked.Set(new Networked()
			{
				idx = idx
			});

			return objRef;
		}

		public static void CreateCamera(this HengineServerEcs.Main world, Camera camera, Vector3 position, int idx)
		{
			var objRef = world.Create(new HengineServerEcs.Cam());
			HengineServerEcs.Cam.Ref entRef = world.Get(objRef);
			entRef.Camera.Set(camera);
			entRef.Position.Set(position);
			entRef.Rotation.Set(Quaternion.Identity);
			entRef.Networked.Set(new Networked()
			{
				idx = idx
			});
		}

		public static EnCS.ArchRef<GuiElement> CreateGuiElement(this HengineEcs.Overlay world, Vector4 position, Vector4 size, TextureAtlas textureAtlas, GuiProperties properties)
		{
			var objRef = world.Create(new GuiElement());
			GuiElement.Ref entRef = world.Get(objRef);
			entRef.GuiPosition.Set(position);
			entRef.GuiSize.Set(size);
			entRef.TextureAtlas.Set(textureAtlas);
			entRef.GuiProperties.Set(properties);

			return objRef;
		}

		public static EnCS.ArchRef<TextElement> CreateTextElement(this HengineEcs.Overlay world, Vector4 position, TextureAtlas textureAtlas, GuiText text)
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