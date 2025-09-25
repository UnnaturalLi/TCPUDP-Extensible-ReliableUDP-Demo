using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using NetworkBase;
namespace UDPServer
{
    public abstract class UDPServer 
    {
        protected UDPServerSession m_Session;
        protected Thread m_SendThread;
        protected IPEndPoint m_EndPoint;
        protected int m_HeartbeatID;
        protected Dictionary<int , uint> m_Acks = new Dictionary<int , uint>();
        protected Dictionary<int , uint> m_Seqs = new Dictionary<int , uint>();
        protected Dictionary<int , uint> m_AckMaps = new Dictionary<int , uint>();
        protected Dictionary<int , uint> m_NextHandledPackets = new Dictionary<int , uint>();
        protected Dictionary<int,Dictionary<uint,byte[]>> m_PendingACKPackets= new Dictionary<int,Dictionary<uint,byte[]>>();
        protected Dictionary<int,Dictionary<uint,INetPacket>> m_heldBackPackets= new Dictionary<int,Dictionary<uint,INetPacket>>();
        
        public bool Init(IPEndPoint ipEndPoint, int HeartbeatID,int HeartbeatCheckTime,int HeartbeatDropTime,int FrameRate)
        {
            m_HeartbeatID= HeartbeatID;
            this.m_EndPoint = ipEndPoint;
            m_Session=new UDPServerSession();
            byte[] Heatbeat=ToHeartBeatPacket(PacketFactoryBase.Instance.GetPacket(HeartbeatID));
            m_Session.Init(FrameRate, (ReceiveData)OnReceiveData, (ReceiveData)RegisterClient, (ReceiveData)DropClient, Heatbeat,HeartbeatCheckTime,HeartbeatDropTime,ipEndPoint);
            return OnInit();
        }

        public void Start()
        {
            m_Session.Start();
            Logger.LogToTerminal($"UDP Server Start at{m_EndPoint}");
            OnStart();
        }

        public void Stop()
        {
            m_Session.Stop();
            Logger.LogToTerminal($"UDP Server Stop at{m_EndPoint}");
            OnStop();
        }
        protected virtual bool OnInit()
        {
            return true;
        }
        protected virtual void OnStart(){}
        protected virtual void OnStop(){}
        protected void OnReceiveData(int id)
        {
            var data=m_Session.GetDataFromDic(id);
            foreach (var VARIABLE in data)
            {
                var obj=UnPack(VARIABLE,id);
                if (obj != null)
                {
                    OnReceiveObj(id, obj);
                }
            }
        }
        public abstract void OnReceiveObj(int id, INetPacket obj);
        public abstract void OnRegisterClient(int id);
        public abstract void OnDropClient(int id);

        public void DropClient(int id)
        {
            m_Acks.Remove(id);
            m_Seqs.Remove(id);
            m_NextHandledPackets.Remove(id);
            m_PendingACKPackets[id].Clear();
            m_PendingACKPackets.Remove(id);
            m_AckMaps.Remove(id);
            m_heldBackPackets[id].Clear();
            m_heldBackPackets.Remove(id);
            OnDropClient(id);
        }
        public void RegisterClient(int id)
        {
            m_Acks[id] =0;
            m_Seqs[id] = 1;
            m_AckMaps[id] = 0;
            m_NextHandledPackets[id] = 1;
            m_PendingACKPackets[id] = new Dictionary<uint, byte[]>();
            m_heldBackPackets[id] = new Dictionary<uint,INetPacket>();
            OnRegisterClient(id);
        }
        public void SendTo(int id, INetPacket obj)
        {
            var data = ToPacket(obj, id);
            m_Session.AppendToSend(id, data);
            m_PendingACKPackets[id].Add(m_Seqs[id]-1,data);
        }

        public byte[] ToHeartBeatPacket(INetPacket obj)
        {
            byte[] data=obj.ToBytes();
            byte[] returnData=new byte[data.Length+24];
            try
            {
                var header=BitConverter.GetBytes((uint)data.Length);
                Array.Copy(header,0,returnData,0,4);
                Array.Copy(BitConverter.GetBytes(m_HeartbeatID),0,returnData,4,4);
                Array.Copy(BitConverter.GetBytes(0),0,returnData,8,4);
                Array.Copy(BitConverter.GetBytes(0u),0,returnData,12,4);
                Array.Copy(BitConverter.GetBytes(0u),0,returnData,16,4);
                Array.Copy(BitConverter.GetBytes(0u),0,returnData,20,4);
                Array.Copy(data, 0, returnData,24,data.Length);
                return returnData;
            }
            catch (Exception e)
            {
                Logger.LogToTerminal(e.Message);
                return null;
            }
        }
        public byte[] ToPacket(INetPacket obj,int id)
        {
            byte[] data=obj.ToBytes();
            byte[] returnData=new byte[data.Length+24];
            try
            {
                var header=BitConverter.GetBytes((uint)data.Length);
                Array.Copy(header,0,returnData,0,4);
                Array.Copy(BitConverter.GetBytes(PacketFactoryBase.Instance.GetPacketID(obj.GetType())),0,returnData,4,4);
                Array.Copy(BitConverter.GetBytes(0),0,returnData,8,4);
                Array.Copy(BitConverter.GetBytes(m_Acks[id]),0,returnData,12,4);
                Array.Copy(BitConverter.GetBytes(m_Seqs[id]),0,returnData,16,4);
                Array.Copy(BitConverter.GetBytes(m_AckMaps[id]),0,returnData,20,4);
                Array.Copy(data, 0, returnData,24,data.Length);
                m_Seqs[id]++;
                return returnData;
            }
            catch (Exception e)
            {
                Logger.LogToTerminal(e.Message);
                return null;
            }
        }
        public INetPacket UnPack(byte[] data,int id)
        {
            uint length=BitConverter.ToUInt32(data,0);
            int packID=BitConverter.ToInt32(data,4);
            int flag=BitConverter.ToInt32(data,8);
            uint ack=BitConverter.ToUInt32(data,12);
            
            uint seq=BitConverter.ToUInt32(data,16);
            uint ackmap=BitConverter.ToUInt32(data,20);
            UpdateACK(ack,id);
            CheckResending(ack,ackmap,id);
            SendACK(id);
            INetPacket obj = PacketFactoryBase.Instance.GetPacket(packID);
            if (flag == 0)
            {
                byte[] packBuffer = new byte[length];
                Array.Copy(data, 24, packBuffer, 0, length);
                obj.FromBytes(packBuffer);
                return obj;
            }

            return null;
        }

        public void SendACK(int id)
        {
            m_Session.UpdateACK(m_Acks[id],m_AckMaps[id],id);
        }
        public void UpdateACK(uint ack,int id)
        {
            if (ack > m_Acks[id])
            {
                uint lastAck = m_Acks[id];
                m_Acks[id] = ack;
                m_AckMaps[id] = m_AckMaps[id] << (int)(ack - lastAck);
                m_AckMaps[id] |= 1u;
            }
            else
            {
                uint lastAck = m_Acks[id];
                m_AckMaps[id] |= 1u<<(int)(lastAck - ack);
            }
        }
        public void CheckResending(uint ack,uint ackMap,int id)
        {
            int count = (ack < 32u) ? (int)(ack) : 32;
            for (int i = 0; i < count; i++)
            {
                if ((ackMap&1u<<i)==(1u<<i))
                {
                    if (m_PendingACKPackets[id].ContainsKey((uint)(ack - i)))
                    {
                        m_PendingACKPackets[id].Remove((uint)(ack - i));
                    }
                }
                else
                {
                    try
                    {
                        m_Session.AppendToSend(id, m_PendingACKPackets[id][(uint)(ack - i)]);
                    }
                    catch (Exception e)
                    {
                        Logger.LogToTerminal("No packet Found "+e.Message);
                    }
                    
                }
            }
        }
        public void Broadcast(INetPacket obj)
        {
            List<int> ids = m_Session.GetClientIDs();
            for (int i = 0; i < ids.Count; i++)
            {
                SendTo(ids[i], obj);
            }
        }
    }
}