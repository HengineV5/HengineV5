namespace Engine.Parsing
{
	public struct GlyphData
	{
		public GlyphDescription glyphDescription;

		public ushort[] endPtsOfContours;
		public int[] xCoords;
		public int[] yCoords;
	}
}
