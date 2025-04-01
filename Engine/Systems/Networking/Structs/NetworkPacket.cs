namespace Engine
{
	public enum PacketType : byte
	{
		Info,
		Update
	}

	public struct NetworkPacket
	{
		public PacketType type;
	}

	public struct UpdatePacket
	{
		public int idx;
		public Vector3f position;
		public Quaternionf roation;
	}
}
