using Hengine.Utils.Parsing.TTF;

namespace Hengine.Graphics
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
