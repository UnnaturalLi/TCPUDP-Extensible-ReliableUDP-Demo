using System;
using NetworkBase;
using System.Collections.Generic;
namespace UDPClient
{
    public abstract class UDPClient : ClientBase
    {
        public Queue<INetPacket> dataQueue;
        public bool isRunning;
        protected override bool OnInit()
        {
            dataQueue = new Queue<INetPacket>();
            
            session = new UDPSession();
            (session as UDPSession).OnDataReceived += Receive;
            if (!session.Init(m_Addr))
            {
                return false;
            }
            base.OnInit();
            return true;
        }
        protected override void OnStart()
        {
            isRunning = true;
            base.OnStart();
        }

        protected override void OnStop()
        {
            isRunning = false;
            base.OnStop();
        }

        public void Send(INetPacket packet)
        {
            try
            {
                var packetData = packet.ToBytes();
                var data=new byte[packetData.Length+8];
                Array.Copy(BitConverter.GetBytes((uint)packetData.Length),0,data,0,4);
                Array.Copy(BitConverter.GetBytes(PacketFactoryBase.Instance.GetPacketID(packet.GetType())),0,data,4,4);
                Array.Copy(packetData,0,data,8,packetData.Length);
                (session as UDPSession).AppendToSendQueue(data);
            }
            catch (Exception e)
            {
                Logger.LogToTerminal(e.Message);
            }
            
        }

        protected abstract void OnReceive();

        private void Receive()
        {
            var newData=(session as UDPSession).GetReceivedData();
            bool newPacket = false;
            foreach (var packetByte in newData)
            {
                if (packetByte.Length < 8)
                {
                    continue;
                }

                try
                {
                    byte[] buffer = new byte[4];
                    Array.Copy(packetByte,0,buffer,0,4);
                    uint length=BitConverter.ToUInt32(buffer,0);
                    Array.Copy(packetByte,4,buffer,0,4);
                    int typeID=BitConverter.ToInt32(buffer,0);
                    if (length == 0)
                    {
                        INetPacket empty=PacketFactoryBase.Instance.GetPacket(typeID);
                        empty.FromBytes(new byte[0]);
                        lock (dataQueue)
                        {
                            dataQueue.Enqueue(empty);
                        }
                        newPacket = true;
                        continue;
                    }
                    buffer = new byte[length];
                    INetPacket packet = PacketFactoryBase.Instance.GetPacket(typeID);
                    Array.Copy(packetByte,8,buffer,0,length);
                    packet.FromBytes(buffer);
                    lock (dataQueue)
                    {
                        dataQueue.Enqueue(packet);
                    }
                    newPacket = true;
                }
                catch (Exception e)
                {
                    Logger.LogToTerminal(e.Message);
                }
            }
            if (newPacket)
            {
                OnReceive();
            }
        }
    }
}