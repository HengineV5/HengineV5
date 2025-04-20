using EnCS.Attributes;
using Engine.Components;
using Silk.NET.Windowing;
using System.Drawing;

namespace Engine
{
	[System]
	public partial class GuiButtonSystem
	{
		IWindow window;
		IInputHandler inputHandler;

		public GuiButtonSystem(IWindow window, IInputHandler inputHandler)
		{
			this.window = window;
			this.inputHandler = inputHandler;
		}

		[SystemUpdate]
		public void GuiStateUpdate(GuiProperties.Ref properties, GuiPosition.Ref position, GuiSize.Ref size, GuiState.Ref guiState, GuiButton.Ref button)
		{
			Vector2f windowSize = new(window.Size.X, window.Size.Y);
			Vector4f pos = new(position.x, position.y, position.z, position.w);
			Vector4f s = new(size.x, size.y, size.z, size.w);

			Vector2f objScreenPos = GuiHelpers.ToScreenSpace(ref pos, ref windowSize);
			Vector2f objScreenSize = GuiHelpers.ToScreenSpace(ref s, ref windowSize);

			Vector2f mousePos = inputHandler.GetMousePosition();
            
			if (mousePos.x > objScreenPos.x && mousePos.x < objScreenPos.x + objScreenSize.x && mousePos.y > objScreenPos.y && mousePos.y < objScreenPos.y + objScreenSize.y)
			{
				guiState.state = inputHandler.IsKeyDown(Silk.NET.Input.MouseButton.Left) ? button.pressedState : button.hoverState;
			}
			else
				guiState.state = button.normalState;
		}
	}

	[System]
	public partial class GuiDraggableSystem
	{
		IWindow window;
		IInputHandler inputHandler;

		public GuiDraggableSystem(IWindow window, IInputHandler inputHandler)
		{
			this.window = window;
			this.inputHandler = inputHandler;
		}

		[SystemUpdate]
		public void GuiDraggableUpdate(GuiPosition.Ref position, GuiSize.Ref size, GuiDraggable.Ref draggable)
		{
			Vector2f windowSize = new(window.Size.X, window.Size.Y);
			Vector4f pos = new(position.x, position.y, position.z, position.w);
			Vector4f s = new(size.x, size.y, size.z, size.w);

			Vector2f objScreenPos = GuiHelpers.ToScreenSpace(ref pos, ref windowSize);
			Vector2f objScreenSize = GuiHelpers.ToScreenSpace(ref s, ref windowSize);

			Vector2f mousePos = inputHandler.GetMousePosition();

			if (draggable.isDragging == 0 && inputHandler.IsKeyDown(Silk.NET.Input.MouseButton.Left) && (mousePos.x > objScreenPos.x && mousePos.x < objScreenPos.x + objScreenSize.x && mousePos.y > objScreenPos.y && mousePos.y < objScreenPos.y + objScreenSize.y))
			{
				position.x = objScreenPos.x;
				position.y = 0;
				position.z = objScreenPos.y;
				position.w = 0;

				draggable.offsetX = position.x - mousePos.x;
				draggable.offsetY = position.z - mousePos.y;
				draggable.isDragging = 1;
			}
			else if (draggable.isDragging == 1 && !(inputHandler.IsKeyDown(Silk.NET.Input.MouseButton.Left)))
			{
				draggable.offsetX = 0;
				draggable.offsetY = 0;
				draggable.isDragging = 0;
			}
			else if (draggable.isDragging == 1 && inputHandler.IsKeyDown(Silk.NET.Input.MouseButton.Left))
			{
				position.x = mousePos.x + draggable.offsetX;
				position.z = mousePos.y + draggable.offsetY;
			}
		}
	}

	static class GuiHelpers
	{
		public static Vector2f ToScreenSpace(ref readonly Vector4f vec, ref readonly Vector2f screenSize)
		{
			return new Vector2f(vec.x + vec.y * screenSize.x, vec.z + vec.w * screenSize.y);
		}
	}
}
