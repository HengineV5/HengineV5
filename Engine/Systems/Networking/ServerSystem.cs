using EnCS.Attributes;
using Engine.Components;
using Engine.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
	[System]
	public partial class ServerSystem
	{
		IServer server;

        public ServerSystem(IServer server)
        {
            this.server = server;
        }

        public void Init()
        {
			server.AcceptNewClients();
		}

		int skipCount = 0;

		[SystemPreLoop]
        public void UpdateClients()
        {
            server.AcceptData();

			skipCount++;

			if (skipCount == 10000 * 2)
				skipCount = 0;
		}

        [SystemUpdate]
        public void Update(Position.Ref position, Rotation.Ref rotation, Camera.Ref camera, Networked.Ref networked)
        {
            var packets = server.GetPackets();
            List<int> toRemove = new List<int>();
			for (int i = 0; i < packets.Count; i++)
			{
                var packet = packets[i];

                if (packet.idx == networked.idx)
                {
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
		public void Update2(Position.Ref position, Rotation.Ref rotation, Camera.Ref camera, Networked.Ref networked)
		{
			if (skipCount != 0)
				return;

            server.BroadcastPacket(new NetworkPacket()
			{
				idx = networked.idx,
				position = new Vector3(position.x, position.y, position.z),
				roation = new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w)
			});
		}
	}
}
