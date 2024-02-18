using EnCS.Attributes;

namespace Engine.Graphics
{
	[Component]
	public partial struct HexCell
	{
		public float height;

        public HexCell(float height)
        {
            this.height = height;
        }
    }
}
