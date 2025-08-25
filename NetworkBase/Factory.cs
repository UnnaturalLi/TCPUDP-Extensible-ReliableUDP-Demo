using System;
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
        public Dictionary<int, Type> PacketTypeDic = new Dictionary<int, Type>();
        public bool Init()
        {
            return OnInit();
        }
        protected abstract bool OnInit();

        public INetPacket GetPacket(int packetId)
        {
            if (PacketTypeDic.ContainsKey(packetId))
            {
                return (INetPacket)Activator.CreateInstance(PacketTypeDic[packetId]);
            }
            return null;
        }

        public int GetPacketID(Type packetType)
        {
            foreach (var VARIABLE in PacketTypeDic)
            {
                if (VARIABLE.Value == packetType)
                {
                    return VARIABLE.Key;
                }
            }
            return -1;
        }
    }
}