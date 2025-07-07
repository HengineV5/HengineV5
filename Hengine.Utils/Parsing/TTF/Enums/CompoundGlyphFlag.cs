namespace Hengine.Utils.Parsing.TTF
{
	[Flags]
	enum CompoundGlyphFlag : ushort
	{
		Arg1And2AreWords = 1,
		ArgsAreXYValues = 2,
		RoundXYToGrid = 4,
		WeHaveAScale = 8,
		// Obsolete bit, always zero
		MoreComponents = 32,
		WeHaveAnXAndYScale = 64,
		WeHaveATwoByTwo = 128,
		WeHaveInstructions = 256,
		UseMyMetrics = 512,
		OverlapCompound = 1024,
	}
}
