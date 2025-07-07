using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Hengine
{
	public class Client : IClient
	{
		private static readonly JsonSerializerOptions options = new JsonSerializerOptions
		{
			IncludeFields = true
		};

		private static readonly byte[] endSign = new byte[] { 9 };

		ILogger logger;
		int idx;
		Socket socket;
		List<UpdatePacket> packets = new List<UpdatePacket>();

		public Client(ILoggerFactory factory, Socket socket, int idx)
        {
			this.logger = factory.CreateLogger<Client>();
			this.socket = socket;
			this.idx = idx;
        }

		public void AcceptData()
		{
            if (socket.Available <= 0)
				return;

			ReceivePacket(socket);
		}

		void ReceivePacket(Socket socket)
		{
			Span<byte> buff = stackalloc byte[1024];
			int length = socket.ReceiveUntill(buff, endSign);

			if (length == 0)
			{
				logger.LogPacketDiscared(length, Encoding.UTF8.GetString(buff));
				return;
			}

			PacketType packetType = (PacketType)buff[0];
			switch (packetType)
			{
				case PacketType.Info:
					break;
				case PacketType.Update:
					var packet = JsonSerializer.Deserialize(buff.Slice(1, length - 1), NetworkPacketSourceGenerationContext.Default.UpdatePacket);
					packets.Add(packet);
					break;
				default:
					break;
			}
		}

		public List<UpdatePacket> GetPackets()
		{
			return packets;
		}

		public void SendData(UpdatePacket data)
		{
			if (!socket.IsConnected())
				return;

            var bytes = JsonSerializer.SerializeToUtf8Bytes(data, NetworkPacketSourceGenerationContext.Default.UpdatePacket);

			socket.Send([(byte)PacketType.Update]);
			socket.Send(bytes);
			socket.Send(endSign);
		}
	}
}
