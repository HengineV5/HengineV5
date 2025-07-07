namespace Hengine.Utils.Parsing.TTF
{
	[Flags]
	enum SimpleGlyphFlag : byte
	{
		ControlPoint = 0,
		OnCurve = 1,
		XByte = 2,
		YByte = 4,
		Repeat = 8,
		XSignOrSame = 16,
		YSignOrSame = 32
	}
}
