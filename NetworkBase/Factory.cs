using System.Collections.Generic;

namespace NetworkBase
{
    public abstract class PacketFactoryBase
    {
        public static PacketFactoryBase Instance;

        public PacketFactoryBase()
        {
            Instance = this;
        }
        public delegate INetPacket PacketHandler();
        public Dictionary<int, PacketHandler> PacketHandlers = new Dictionary<int, PacketHandler>();
        public bool Init()
        {
            return OnInit();
        }
        protected abstract bool OnInit();

        public INetPacket GetPacket(int packetId)
        {
            if (PacketHandlers.ContainsKey(packetId))
            {
                return PacketHandlers[packetId]?.Invoke();
            }
            return null;
        }
    }
}