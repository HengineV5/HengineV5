using EnCS.Attributes;
using Engine.Components;
using Engine.Graphics;
using System.Numerics;

namespace Engine
{
	[System]
	public partial class ClientSendSystem
    {
		IClient client;

        public ClientSendSystem(IClient client)
        {
			this.client = client;
        }

		int skipCount = 0;

		[SystemPreLoop]
		public void PreUpdate()
		{
			skipCount++;

			if (skipCount >= Random.Shared.Next(5, 10))
				skipCount = 0;
		}

        [SystemUpdate]
		public void Update(Position.Ref position, Rotation.Ref rotation, Camera.Ref camera, Networked.Ref networked)
		{
			if (skipCount != 0)
				return;

			client.SendData(new NetworkPacket()
			{
				idx = networked.idx,
				position = new Vector3(position.x, position.y, position.z),
				roation = new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w)
			});
		}
	}

	[System]
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
		public void Update(Position.Ref position, Rotation.Ref rotation, Scale.Ref scale, Networked.Ref networked)
		{
			var packets = client.GetPackets();
			List<int> toRemove = new List<int>();
			for (int i = 0; i < packets.Count; i++)
			{
				var packet = packets[i];

				if (packet.idx == networked.idx)
				{
					position.Set(packet.position + new Vector3(0, 0, -5));
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
