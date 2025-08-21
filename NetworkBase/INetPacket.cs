namespace NetworkBase
{
    public interface INetPacket
    {
        byte[] ToBytes();
        void FromBytes(byte[] data);
    }
}