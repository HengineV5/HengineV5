using EnCS.Attributes;
using Engine.Components;
using Engine.Graphics;
using Microsoft.Extensions.Logging;

namespace Engine
{
	static partial class LoggerExtensionMethods
	{
		[LoggerMessage(Level = LogLevel.Trace, Message = "Sending: {idx} {position}, {rotation}")]
		public static partial void LogClientSend(this ILogger logger, int idx, Vector3f position, Quaternionf rotation);
	}

	[System]
	[SystemContext<EngineContext>]
	public partial class ClientSendSystem
    {
		ILogger logger;
		IClient client;

        public ClientSendSystem(ILoggerFactory factory, IClient client)
        {
			this.logger = factory.CreateLogger<ClientSendSystem>();
			this.client = client;
        }

		float time = 0;

		[SystemPreLoop]
		public void PreUpdate()
		{
		}

        [SystemUpdate]
		public void Update(ref EngineContext engineContext, Position.Ref position, Rotation.Ref rotation, Camera.Ref camera, Networked.Ref networked)
		{
			time += engineContext.dt;
			if (time < .1f / 20)
				return;

			logger.LogClientSend(networked.idx, new Vector3f(position.x, position.y, position.z), new Quaternionf(rotation.x, rotation.y, rotation.z, rotation.w));

			time = 0;
            client.SendData(new UpdatePacket()
			{
				idx = networked.idx,
				position = new Vector3f(position.x, position.y, position.z),
				roation = new Quaternionf(rotation.x, rotation.y, rotation.z, rotation.w)
			});
		}
	}

	[System]
	[SystemContext<EngineContext>]
	public partial class ClientReceiveSystem
	{
		IClient client;

		public ClientReceiveSystem(IClient client)
		{
			this.client = client;
		}

		[SystemPreLoop]
		public void PreUpdate()
		{
			client.AcceptData();
		}

		[SystemUpdate]
		public void Update(ref EngineContext engineContext, Position.Ref position, Rotation.Ref rotation, Scale.Ref scale, Networked.Ref networked)
		{
            var packets = client.GetPackets();
			List<int> toRemove = new List<int>();
			for (int i = 0; i < packets.Count; i++)
			{
				var packet = packets[i];

				if (packet.idx == networked.idx)
				{
                    //Console.WriteLine($"Found match: {packet.idx} {packet.position}, {packet.roation}");

                    position.Set(packet.position);
					rotation.Set(packet.roation);
					toRemove.Add(i);

                }
			}

			for (int i = 0; i < toRemove.Count; i++)
			{
				packets.RemoveAt(toRemove[i]);
			}
		}
	}
}
