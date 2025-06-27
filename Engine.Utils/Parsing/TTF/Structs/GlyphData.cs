namespace Engine.Utils.Parsing.TTF
{
	public record struct GlyphData
	{
		public GlyphDescription description;
		public Memory<GlyphVertex> coords;
		public Memory<ushort> contours;
		public Memory<ushort> glyphs;

		public GlyphData(GlyphDescription description, Memory<GlyphVertex> coords, Memory<ushort> contours, Memory<ushort> glyphs)
		{
			this.description = description;
			this.coords = coords;
			this.contours = contours;
			this.glyphs = glyphs;
		}
	}

	public record struct GlyphVertex(int x, int y, bool onCurve);
}
