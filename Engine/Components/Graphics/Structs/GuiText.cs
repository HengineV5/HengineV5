using Engine.Utils.Parsing.TTF;

namespace Engine.Graphics
{
	public struct GuiText
    {
        public string id;
        public Font font;

		public GuiText(string id, Font font)
		{
			this.id = id;
			this.font = font;
		}
	}
}
