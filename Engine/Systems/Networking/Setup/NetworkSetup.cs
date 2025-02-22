using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Engine
{
	public interface IServer
	{
		void AcceptNewClients();

		void AcceptData();

		List<NetworkPacket> GetPackets();

		void BroadcastPacket(NetworkPacket packet);
	}

	public interface IClient
	{
		void SendData(NetworkPacket data);

		void AcceptData();

		List<NetworkPacket> GetPackets();
	}

	struct ConnectedClient
	{
		public string name;
		public Socket socket;
	}

	public struct NetworkPacket
	{
		public int idx;
		public Vector3f position;
		public Quaternionf roation;
	}

	[JsonSourceGenerationOptions(IncludeFields = true)]
	[JsonSerializable(typeof(NetworkPacket))]
	public partial class NetworkPacketSourceGenerationContext : JsonSerializerContext
	{

	}

	public class Server : IServer
	{
		private static readonly JsonSerializerOptions options = new JsonSerializerOptions
		{
			IncludeFields = true
		};

		private static readonly byte[] endSign = new byte[] { 9 };

		Socket socket;

		List<ConnectedClient> clients = new List<ConnectedClient>();
		List<NetworkPacket> packets = new List<NetworkPacket>();

        public Server(Socket socket)
        {
			this.socket = socket;
        }

		public void AcceptNewClients()
		{
			for (int i = 0; i < 2; i++)
			{
				Socket client = socket.Accept();

				byte[] b = new byte[100];
				int length = client.Receive(b);

				string name = Encoding.UTF8.GetString(b, 0, length);

				clients.Add(new ConnectedClient()
				{
					name = name,
					socket = client,
				});
			}

            Console.WriteLine($"All clients connected: {clients.Count}.");
        }

		public void AcceptData()
		{
			Span<byte> buff = stackalloc byte[1024];
			for (int i = 0; i < clients.Count; i++)
			{
				ConnectedClient client = clients[i];

				if (client.socket.Available <= 0)
					continue;

				int length = client.socket.Receive(buff);

				for (int a = 0; a < length; a++)
				{
					if (buff[a] == endSign[0])
					{
						length = a;
						break;
					}
				}

				if (length == 0)
					continue;

				try
				{
					var packet = JsonSerializer.Deserialize(buff.Slice(0, length), NetworkPacketSourceGenerationContext.Default.NetworkPacket);
					packets.Add(packet);

                    Console.WriteLine($"Added packet from {i}: {packet.idx}, {packet.position}, {packet.roation}");
                }
				catch (Exception e)
				{
                    //Console.WriteLine($"Failed!: {e.Message}");
                }
			}
		}

		public List<NetworkPacket> GetPackets()
		{
			return packets;
		}

		public void BroadcastPacket(NetworkPacket packet)
		{
			for (int i = 0; i < clients.Count; i++)
			{
				var client = clients[i];

				var bytes = JsonSerializer.SerializeToUtf8Bytes(packet, NetworkPacketSourceGenerationContext.Default.NetworkPacket);
				client.socket.Send(bytes);
				client.socket.Send(endSign);
			}
		}
	}

	public class Client : IClient
	{
		private static readonly JsonSerializerOptions options = new JsonSerializerOptions
		{
			IncludeFields = true
		};

		private static readonly byte[] endSign = new byte[] { 9 };

		int idx;
		Socket socket;
		List<NetworkPacket> packets = new List<NetworkPacket>();

		public Client(Socket socket, int idx)
        {
			this.socket = socket;
			this.idx = idx;
        }

		public void AcceptData()
		{
            if (socket.Available <= 0)
				return;

			Span<byte> buff = stackalloc byte[1024];
			int length = socket.Receive(buff);

			for (int i = 0; i < length; i++)
			{
				if (buff[i] == endSign[0])
				{
					length = i;
					break;
				}
			}

			if (length == 0)
				return;

			try
			{
				var packet = JsonSerializer.Deserialize(buff.Slice(0, length), NetworkPacketSourceGenerationContext.Default.NetworkPacket);
				//Console.WriteLine($"Added packet from server with id {packet.idx}");
				packets.Add(packet);
			}
			catch (Exception e)
			{
				//Console.WriteLine($"Client failed!: {e.Message}");
			}
		}

		public List<NetworkPacket> GetPackets()
		{
			return packets;
		}

		public void SendData(NetworkPacket data)
		{
            var bytes = JsonSerializer.SerializeToUtf8Bytes(data, NetworkPacketSourceGenerationContext.Default.NetworkPacket);
			socket.Send(bytes);
			socket.Send(endSign);
		}
	}

	public class NetworkConfig
	{
		public IPAddress ipAddress;
		public int port;
	}

	public class NetworkSetup
	{
		public static IServer ServerSetup(NetworkConfig networkConfig)
		{
			EndPoint endPoint = new IPEndPoint(networkConfig.ipAddress, networkConfig.port);

			Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			socket.Bind(endPoint);
			socket.Listen(100);

			return new Server(socket);
		}

		public static IClient ClientSetup(NetworkConfig networkConfig, EngineConfig engineConfig)
		{
			EndPoint endPoint = new IPEndPoint(networkConfig.ipAddress, networkConfig.port);

			Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			socket.Connect(endPoint);

			socket.Send(Encoding.UTF8.GetBytes("Test Client."));

            Console.WriteLine("Connected to server!");

            return new Client(socket, engineConfig.idx);
		}
	}
}
