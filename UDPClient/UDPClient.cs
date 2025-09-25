/*
 * Pack:
 * [uint length 4]
 * [int packetID 4]
 * [int flag 4] 0 for normal Packet(seq and ack included) 1 for pure ACK packet
 * [uint ack 4]
 * [uint seq 4]
 * [uint ackMap 4]
 * [body byte[length]]
 */
using System;
using NetworkBase;
using System.Collections.Generic;
namespace UDPClient
{
    public abstract class UDPClient : ClientBase
    {
        public Queue<INetPacket> dataQueue;
        public bool isRunning;
        protected uint m_Ack;
        protected uint m_Seq;
        protected Dictionary<uint,byte[]> m_PendingACKPackets = new Dictionary<uint, byte[]>();
        protected Dictionary<uint,INetPacket> m_heldBackPackets = new Dictionary<uint, INetPacket>();
        protected uint m_AckMap;
        protected uint m_NextHandledPacket;
        protected int m_Heartbeat;
        protected override bool OnInit(params object[] args)
        {
            m_Heartbeat= (int)args[0];
            dataQueue = new Queue<INetPacket>();
            session = new UDPSession();
            m_NextHandledPacket = 1;
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
            m_Ack = 0;
            m_Seq = 1;
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
                var data=new byte[packetData.Length+24];
                Array.Copy(BitConverter.GetBytes((uint)packetData.Length),0,data,0,4);
                Array.Copy(BitConverter.GetBytes(PacketFactoryBase.Instance.GetPacketID(packet.GetType())),0,data,4,4);
                Array.Copy(BitConverter.GetBytes(0),0,data,8,4);
                Array.Copy(BitConverter.GetBytes(m_Ack),0,data,12,4);
                Array.Copy(BitConverter.GetBytes(m_Seq),0,data,16,4);
                Array.Copy(BitConverter.GetBytes(m_AckMap),0,data,20,4);
                Array.Copy(packetData,0,data,24,packetData.Length);
                m_PendingACKPackets.Add(m_Seq,data);
                m_Seq++;
                (session as UDPSession).AppendToSendQueue(data);
            }
            catch (Exception e)
            {
                Logger.LogToTerminal(e.Message);
            }
            
        }

        public void UpdateACK(uint newAck,uint lastAck)
        {
            
            if (newAck > lastAck)
            {
                m_Ack = newAck;
                m_AckMap = m_AckMap << (int)(newAck - lastAck);
                m_AckMap |= 1u;
            }
            else
            {
                m_AckMap |= 1u<<(int)(lastAck - newAck);
            }

        }
        public void CheckResending(uint ack, uint map)
        {
            int count = (ack < 32u) ? (int)(ack) : 32;
            for (int i = 0; i < count; i++)
            {
                if ((map&1u<<i)==(1u<<i))
                {
                    if (m_PendingACKPackets.ContainsKey((uint)(ack - i)))
                    {
                        m_PendingACKPackets.Remove((uint)(ack - i));
                    }
                }
                else
                {
                    try
                    {
                        (session as UDPSession).AppendToSendQueue(m_PendingACKPackets[(uint)(ack - i)]);
                    }
                    catch (Exception e)
                    {
                       Logger.LogToTerminal("No packet Found "+e.Message);
                    }
                    
                }
            }
        }
        protected abstract void OnReceive();

        
        private void Receive()
        {
            var newData=(session as UDPSession).GetReceivedData();
            bool newPacket = false;
            uint oldAck = m_Ack;
            uint oldMap= m_AckMap;
            foreach (var packetByte in newData)
            {
                if (packetByte.Length < 24)
                {
                    continue;
                }
                try
                {
                    uint length=BitConverter.ToUInt32(packetByte,0);
                    int typeID=BitConverter.ToInt32(packetByte,4);
                    int flag=BitConverter.ToInt32(packetByte,8);
                    uint ack=BitConverter.ToUInt32(packetByte,12);
                    uint seq=BitConverter.ToUInt32(packetByte,16);
                    uint ackMap=BitConverter.ToUInt32(packetByte,20);
                    if (typeID == m_Heartbeat)
                    {
                        var hpacket=PacketFactoryBase.Instance.GetPacket(typeID);
                        var hbuffer = new byte[length];
                        Array.Copy(packetByte,24,hbuffer,0,length);
                        hpacket.FromBytes(hbuffer);
                        lock (dataQueue)
                        {
                            dataQueue.Enqueue(hpacket);
                            OnReceive();
                        }
                        continue;
                    }
                    CheckResending(ack,ackMap);
                    if (flag == 1)
                    {
                        continue;
                    }
                    UpdateACK(seq,m_Ack);
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
                    byte[] buffer = new byte[length];
                    INetPacket packet = PacketFactoryBase.Instance.GetPacket(typeID);
                    Array.Copy(packetByte,24,buffer,0,length);
                    packet.FromBytes(buffer);
                    if (seq == m_NextHandledPacket)
                    {
                        lock (dataQueue)
                        {
                            dataQueue.Enqueue(packet);
                        }
                        m_NextHandledPacket++;
                        newPacket = true;
                        INetPacket pending;
                        while (m_heldBackPackets.TryGetValue(m_NextHandledPacket, out pending))
                        {
                            lock (dataQueue)
                            {
                                dataQueue.Enqueue(pending);
                            }
                            m_heldBackPackets.Remove(m_NextHandledPacket);
                            m_NextHandledPacket++;
                        }
                    }
                    else
                    {
                        m_heldBackPackets[seq]=packet;
                    }
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
            if (oldAck != m_Ack || oldMap != m_AckMap)
            {
                (session as UDPSession).UpdateAck(m_Ack, m_AckMap);
            }
        }
    }
}