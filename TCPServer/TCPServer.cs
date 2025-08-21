using System;
using System.Collections.Generic;
using System.Net;
using NetworkBase;

namespace TCPServer
{
    public class ReadInfo
    {
        
        public uint Wrote;
        public uint Read;

        public ReadInfo()
        {
            Wrote = 0;
            Read = 0;
        }
    }
    public abstract class TCPServer<T> where T : INetPacket, new()
    {
        protected TCPServerSession m_Session;
        protected IPEndPoint m_EndPoint;
        protected Dictionary<int, byte[]> m_ReceiveBuffers = new Dictionary<int, byte[]>();
        protected Dictionary<int, ReadInfo> m_ReadInfos = new Dictionary<int, ReadInfo>();
        public bool Init(IPEndPoint ipEndPoint)
        {
            m_EndPoint = ipEndPoint;
            m_Session = new TCPServerSession();
            m_Session.Init(60, (ReceiveData)OnReceiveData, ipEndPoint);
            return OnInit();
        }
        public void Start()
        {
            Logger.LogToTerminal($"TCP Server Start at{m_EndPoint}");
            m_Session.Start();
        }
        public void Stop()
        {
            Logger.LogToTerminal($"TCP Server Stop at{m_EndPoint}");
            m_Session.Stop();
        }
        public virtual bool OnInit()
        {
            return true;
        }

        public void OnReceiveData(int id)
        {
            if (!m_ReceiveBuffers.ContainsKey(id))
            {
                m_ReceiveBuffers.Add(id, new byte[1024]);
                m_ReadInfos.Add(id, new ReadInfo());
            }
            var data=m_Session.GetRecvDataBuffer(id);
            for (int i = 0; i < data.Length; i++)
            {
                for (int j = 0; j < data[i].Length; j++)
                {
                    m_ReceiveBuffers[id][m_ReadInfos[id].Wrote] = data[i][j];
                    m_ReadInfos[id].Wrote++;
                    m_ReadInfos[id].Wrote%= 1024;
                }
            }

            while ((m_ReadInfos[id].Wrote % 1024 + 1024 - m_ReadInfos[id].Read % 1024) % 1024 >= 4)
            {
                byte[] header = new byte[4];
                for (int i = 0; i < 4; i++)
                {
                    header[i] = m_ReceiveBuffers[id][(m_ReadInfos[id].Read+i)%1024];
                }
                uint length=BitConverter.ToUInt32(header,0);
                if (length == 0)
                {
                    m_ReadInfos[id].Read += 4;
                    continue;
                }
                if ((m_ReadInfos[id].Wrote % 1024 + 1024 - m_ReadInfos[id].Read % 1024) % 1024 >= length + 4)
                {
                    m_ReadInfos[id].Read += 4;
                    byte[] packet=new byte[length];
                    for (int i = 0; i < length; i++)
                    {
                        packet[i] = m_ReceiveBuffers[id][m_ReadInfos[id].Read];
                        m_ReadInfos[id].Read++;
                        m_ReadInfos[id].Read%= 1024;
                    }
                    T obj = UnPack(packet);
                    OnReceiveObj(id,obj);
                }
            }
            
        }
        public abstract void OnReceiveObj(int id, T obj);
        
        public void SendTo(int id, T obj)
        {
            m_Session.AppendToSend(id, ToPacket(obj));
        }

        public byte[] ToPacket(T obj)
        {
            byte[] data=obj.ToBytes();
            byte[] returnData=new byte[data.Length+4];
            var header=BitConverter.GetBytes(data.Length);
            Array.Copy(header,0,returnData,0,4);
            Array.Copy(data, 0, returnData,4,data.Length);
            return returnData;
        }
        public T UnPack(byte[] data)
        {
            T obj = new T();
            obj.FromBytes(data);
            return obj;
        }
        public void Broadcast(T obj)
        {
            int n = m_Session.GetClientCount();
            for (int i = 0; i < n; i++)
            {
                SendTo(i, obj);
            }
        }
    }
    
}