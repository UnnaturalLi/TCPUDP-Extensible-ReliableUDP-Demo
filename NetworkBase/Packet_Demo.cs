using System;

namespace NetworkBase
{
    public class Packet_Demo: INetPacket
    {
        public int x;
        public int y;
        public byte[] ToBytes()
        {
            UInt32 n= 4;
           byte[] data = new byte[8];
           Array.Copy(BitConverter.GetBytes(x), 0, data, 0, 4);
           Array.Copy(BitConverter.GetBytes(y), 0, data, 4, 4);
           return data;
        }

        public void FromBytes(byte[] data)
        {
            if (data.Length != 8)
            {
                return;
            }
            x = BitConverter.ToInt32(data, 0);
            y = BitConverter.ToInt32(data, 4); 
        }
    }
}