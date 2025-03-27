using EnCS.Attributes;
using Engine.Components;
using Engine.Graphics;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
	static partial class LoggerExtensionMethods
	{
		[LoggerMessage(Level = LogLevel.Trace, Message = "Found match: {idx} {position}, {rotation}")]
		public static partial void LogMatchFound(this ILogger logger, int idx, Vector3f position, Quaternionf rotation);
	}

	[System]
	[SystemContext<EngineContext>]
	public partial class ServerSystem
	{
        ILogger logger;
		IServer server;

        public ServerSystem(ILoggerFactory factory, IServer server)
        {
            this.logger = factory.CreateLogger<ServerSystem>();
			this.server = server;
        }

        public void Init()
        {
			server.AcceptNewClients();
		}

		float time = 0;

		[SystemPreLoop]
        public void UpdateClients()
        {
            server.AcceptData();
		}

        [SystemUpdate]
        public void Update(ref EngineContext engineContext, Position.Ref position, Rotation.Ref rotation, Camera.Ref camera, Networked.Ref networked)
        {
            var packets = server.GetPackets();
            List<int> toRemove = new List<int>();
			for (int i = 0; i < packets.Count; i++)
			{
                var packet = packets[i];

                if (packet.idx == networked.idx)
                {
                    logger.LogMatchFound(packet.idx, packet.position, packet.roation);

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

		[SystemUpdate]
		public void Update2(ref EngineContext engineContext, Position.Ref position, Rotation.Ref rotation, Camera.Ref camera, Networked.Ref networked)
		{
            time += MathF.Max(engineContext.dt, 0.00001f);
            if (time < 1.12f / 20)
				return;

			time = 0;

            //Console.WriteLine($"BroadcastingL: {new Vector3(position.x, position.y, position.z)} {new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w)}");
            server.BroadcastPacket(new NetworkPacket()
			{
				idx = networked.idx,
				position = new Vector3f(position.x, position.y, position.z),
				roation = new Quaternionf(rotation.x, rotation.y, rotation.z, rotation.w)
			});
		}
	}
}
