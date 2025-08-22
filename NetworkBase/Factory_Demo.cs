namespace NetworkBase
{
    public class Factory_Demo: PacketFactoryBase
    {
        protected override bool OnInit()
        {
            PacketHandlers.Add(0,()=>new Packet_Demo());
            return true;
        }
    }
}