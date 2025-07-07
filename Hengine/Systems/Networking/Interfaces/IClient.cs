namespace Hengine
{
	public interface IClient
	{
		void SendData(UpdatePacket data);

		void AcceptData();

		List<UpdatePacket> GetPackets();
	}
}
