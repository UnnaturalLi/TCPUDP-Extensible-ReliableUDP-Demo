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
        public bool Init(IPEndPoint ipEndPoint, int HeartbeatID,int HeartbeatCheckTime,int HeartbeatDropTime,int FrameRate)
        {
            this.m_EndPoint = ipEndPoint;
            m_Session=new UDPServerSession();
            byte[] Heatbeat=ToPacket(PacketFactoryBase.Instance.GetPacket(HeartbeatID));
            m_Session.Init(FrameRate, (ReceiveData)OnReceiveData, (ReceiveData)OnRegisterClient, (ReceiveData)OnDropClient, Heatbeat,HeartbeatCheckTime,HeartbeatDropTime,ipEndPoint);
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
                var obj=UnPack(VARIABLE);
                if (obj != null)
                {
                    OnReceiveObj(id, obj);
                }
            }
        }
        public abstract void OnReceiveObj(int id, INetPacket obj);
        public abstract void OnRegisterClient(int id);
        public abstract void OnDropClient(int id);
        public void SendTo(int id, INetPacket obj)
        {
            m_Session.AppendToSend(id, ToPacket(obj));
        }
        public byte[] ToPacket(INetPacket obj)
        {
            byte[] data=obj.ToBytes();
            byte[] returnData=new byte[data.Length+8];
            try
            {
                var header=BitConverter.GetBytes((uint)data.Length);
                Array.Copy(header,0,returnData,0,4);
                Array.Copy(BitConverter.GetBytes(PacketFactoryBase.Instance.GetPacketID(obj.GetType())),0,returnData,4,4);
                Array.Copy(data, 0, returnData,8,data.Length);
                return returnData;
            }
            catch (Exception e)
            {
                Logger.LogToTerminal(e.Message);
                return null;
            }
        }
        public INetPacket UnPack(byte[] data)
        {
            byte[] headerBuffer=new byte[4];
            Array.Copy(data,0,headerBuffer,0,4);
            uint length=BitConverter.ToUInt32(headerBuffer,0);
            if (length == 0)
            {
                return null;
            }
            Array.Copy(data,4,headerBuffer,0,4);
            int packID=BitConverter.ToInt32(headerBuffer,0);
            INetPacket obj = PacketFactoryBase.Instance.GetPacket(packID);
            byte[] packBuffer = new byte[length];
            Array.Copy(data,8,packBuffer,0,length);
            obj.FromBytes(packBuffer);
            return obj;
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