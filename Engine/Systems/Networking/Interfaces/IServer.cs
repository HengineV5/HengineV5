namespace Engine
{
	public interface IServer
	{
		void StartServerThread();

		void AcceptData();

		List<UpdatePacket> GetPackets();

		void BroadcastPacket(UpdatePacket packet);
	}
}
