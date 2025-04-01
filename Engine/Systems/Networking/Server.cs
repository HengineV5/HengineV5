using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace Engine
{
	public static class NetworkUtils
	{
		public static int ReceiveUntill(this Socket socket, Span<byte> buff, Span<byte> endSign)
		{
			bool found = false;
			int scanned = 0;
			while (!found)
			{
				int length = socket.Receive(buff.Slice(scanned));

				for (int i = 0; i < length; i++)
				{
					if (buff[scanned] == endSign[0])
					{
						found = true;
						break;
					}

					scanned++;
				}
			}

			return scanned;
		}

		public static bool IsConnected(this Socket socket)
		{
			try
			{
				return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
			}
			catch (SocketException) { return false; }
		}
	}

	public class Server : IServer
	{
		private static readonly JsonSerializerOptions options = new JsonSerializerOptions
		{
			IncludeFields = true
		};

		private static readonly byte[] endSign = new byte[] { 9 };

		Lock l;
		ILogger logger;
		Socket socket;

		List<ConnectedClient> clients = new List<ConnectedClient>();
		List<UpdatePacket> packets = new List<UpdatePacket>();

        public Server(ILoggerFactory factory, Socket socket)
        {
			this.l = new();
			this.logger = factory.CreateLogger<Server>();
			this.socket = socket;
        }

		public void StartServerThread()
		{
			var thread = new Thread(AcceptClientsThread);
			thread.Start();

			logger.LogInformation("Server active and listening.");
		}

		void AcceptClientsThread()
		{
			Span<byte> buff = stackalloc byte[128];
			while (true)
			{
				Socket client = socket.Accept();

				if (!client.Poll(100, SelectMode.SelectRead))
				{
					logger.LogClientDidNotProvideName(client.LocalEndPoint?.ToString());
					continue;
				}

				int length = client.Receive(buff);
				string name = Encoding.UTF8.GetString(buff.Slice(0, length));

				lock (l)
				{
					clients.Add(new ConnectedClient()
					{
						name = name,
						socket = client,
					});

					logger.LogClientConnected(socket.LocalEndPoint?.ToString(), name);
				}
			}
		}

		public void AcceptData()
		{
			lock (l)
			{
				for (int i = 0; i < clients.Count; i++)
				{
					ConnectedClient client = clients[i];

					if (!client.socket.IsConnected())
					{
						logger.LogClientNoLongerConnected(client.socket.LocalEndPoint?.ToString(), client.name);

						clients.RemoveAt(i);
						continue;
					}

					if (client.socket.Available <= 0)
						continue;

					ReceivePacket(i, client.socket);
				}
			}
		}

		void ReceivePacket(int clientID, Socket socket)
		{
			Span<byte> buff = stackalloc byte[2048];
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

					logger.LogPacketAdded(clientID, packet.idx, packet.position, packet.roation);
					break;
				default:
					break;
			}
		}

		public List<UpdatePacket> GetPackets()
		{
			return packets;
		}

		public void BroadcastPacket(UpdatePacket packet)
		{
			for (int i = 0; i < clients.Count; i++)
			{
				var client = clients[i];

				if (!client.socket.IsConnected())
					continue;

				var bytes = JsonSerializer.SerializeToUtf8Bytes(packet, NetworkPacketSourceGenerationContext.Default.UpdatePacket);
				client.socket.Send([(byte)PacketType.Update]);
				client.socket.Send(bytes);
				client.socket.Send(endSign);

				//logger.LogInformation($"Sending packet {Encoding.UTF8.GetString(bytes)}");
			}
		}
	}
}
