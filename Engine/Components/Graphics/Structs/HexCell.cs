using EnCS.Attributes;

namespace Engine.Graphics
{
	[Component]
	public ref partial struct HexCell
	{
		public ref float height;
    }

	public struct HexCellData
	{
		public float height;

		public HexCellData(float height)
		{
			this.height = height;
		}
	}
}
