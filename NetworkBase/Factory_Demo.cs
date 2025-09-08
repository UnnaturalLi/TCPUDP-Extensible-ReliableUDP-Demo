namespace NetworkBase
{
    public class Factory_Demo: PacketFactoryBase
    {
        protected override bool OnInit()
        {
            PacketTypeDic.Add(0,typeof(Packet_Demo));
            PacketTypeDic.Add(1,typeof(RTTPacket_Demo));
            PacketTypeDic.Add(2,typeof(HeartbeatPacket_Demo));
            return true;
        }
    }
    public class Factory_Demo_UDP: PacketFactoryBase
    {
        protected override bool OnInit()
        {
            PacketTypeDic.Add(1,typeof(Packet_Demo));
            PacketTypeDic.Add(2,typeof(RTTPacket_Demo));
            PacketTypeDic.Add(0,typeof(HeartbeatPacket_Demo));
            return true;
        }
    }

}