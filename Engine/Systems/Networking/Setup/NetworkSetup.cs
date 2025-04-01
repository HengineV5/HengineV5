using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Serialization;

namespace Engine
{
	static partial class LoggerExtensionMethods
	{
		[LoggerMessage(Level = LogLevel.Information, Message = "Clients connected: {clients}")]
		public static partial void LogClientsConnected(this ILogger logger, int clients);

		[LoggerMessage(Level = LogLevel.Information, Message = "Client {ip} '{client}' connected.")]
		public static partial void LogClientConnected(this ILogger logger, string ip, string client);

		[LoggerMessage(Level = LogLevel.Trace, Message = "Added packet from {i}: {idx}, {position}, {rotation}")]
		public static partial void LogPacketAdded(this ILogger logger, int i, int idx, Vector3f position, Quaternionf rotation);

		[LoggerMessage(Level = LogLevel.Warning, Message = "Client {ip} connected but did not provide a name, was not registerd.")]
		public static partial void LogClientDidNotProvideName(this ILogger logger, string ip);

		[LoggerMessage(Level = LogLevel.Warning, Message = "Packet of length {length} discared: '{packet}'")]
		public static partial void LogPacketDiscared(this ILogger logger, int length, string packet);

		[LoggerMessage(Level = LogLevel.Information, Message = "Client {ip} '{client}' is no longer connected.")]
		public static partial void LogClientNoLongerConnected(this ILogger logger, string ip, string client);
	}

	[JsonSourceGenerationOptions(IncludeFields = true)]
	[JsonSerializable(typeof(NetworkPacket))]
	[JsonSerializable(typeof(UpdatePacket))]
	public partial class NetworkPacketSourceGenerationContext : JsonSerializerContext
	{

	}

	public class NetworkConfig
	{
		public IPAddress ipAddress;
		public int port;
	}

	public class NetworkSetup
	{
		public static IServer ServerSetup(ILoggerFactory factory, NetworkConfig networkConfig)
		{
			EndPoint endPoint = new IPEndPoint(networkConfig.ipAddress, networkConfig.port);

			Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			socket.Bind(endPoint);
			socket.Listen(100);

			socket.ReceiveTimeout = 100;

			return new Server(factory, socket);
		}

		public static IClient ClientSetup(ILoggerFactory factory, NetworkConfig networkConfig, EngineConfig engineConfig)
		{
			EndPoint endPoint = new IPEndPoint(networkConfig.ipAddress, networkConfig.port);

			Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			socket.Connect(endPoint);

			socket.Send(Encoding.UTF8.GetBytes("Test Client."));

			return new Client(factory, socket, engineConfig.idx);
		}
	}
}
