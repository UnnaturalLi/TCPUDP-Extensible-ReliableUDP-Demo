using System;
using System.Net.Sockets;
using System.Threading;
using NetworkBase;
using System.Collections.Generic;
using System.Net;
using TCPServer;
using UDPClient;

namespace UDPServer
{
    public class UDPClientInfo
    {
        public DateTime lastTime { get; set; }
        public int id {get;set;}
        public IPEndPoint ip {get;set;}
        public void SetID(int id)
        {
            this.id = id;
        }

        public void SetIPAddress(IPEndPoint ip)
        {
            this.ip = ip;
        }
        public void SetLastTime(DateTime time)
        {
            lastTime = time;
        }
        public void SetLastTime()
        {
            lastTime = DateTime.Now;
        }
    }
    public class UDPServerSession : AppBase
    {
        public ReceiveData OnReceive;
        protected Dictionary<int, UDPClientInfo> m_ClientsDic = new Dictionary<int, UDPClientInfo>();
        protected AutoResetEvent m_SendDataAutoResetEvent = new AutoResetEvent(false);
        protected UdpClient m_Client;
        protected int m_ClientNextID;
        protected Dictionary<int,Queue<byte[]>> m_RecvQueue = new Dictionary<int,Queue<byte[]>>();
        protected Dictionary<int,Queue<byte[]>> m_SendQueue = new Dictionary<int,Queue<byte[]>>();
        protected Thread m_SendThread;
        public override bool OnInit(params object[] args)
        {
            OnReceive = (ReceiveData) args[0];
            m_ClientNextID = 0;
            try
            {
                m_Client = new UdpClient(args[1] as IPEndPoint);
                return true;
            }
            catch (Exception e)
            {
                Logger.LogToTerminal("error: " + e);
            }
            return false;
        }

        public override void OnStart()
        {
            base.OnStart();
            m_SendThread = CreateThread(T_Send);
        }

        public override void OnRun()
        {
            Receive();
        }
        public override void OnStop()
        {
            base.OnStop();
            m_Client.Close();
            m_IsRunning = false;
        }
        public void T_Send()
        {
            while (true)
            {
                if (!m_IsRunning)
                {
                    m_SendDataAutoResetEvent.WaitOne(1000);
                    continue;
                }
                bool sent = false;
                foreach (var VARIABLE in m_SendQueue)
                {
                    if (VARIABLE.Value.Count > 0)
                    {
                        var data = VARIABLE.Value.Dequeue();
                        try
                        {
                            m_Client.Send(data,data.Length,m_ClientsDic[VARIABLE.Key].ip);
                        }
                        catch (Exception e)
                        {
                            Logger.LogToTerminal("Sending error: " + e);
                        }
                        sent = true;
                    }
                }
                if (!sent)
                {
                    m_SendDataAutoResetEvent.WaitOne(1000);
                }
            }
        }
        public void AppendToSend(int id,byte[] data)
        {
            try
            {
                lock (m_SendQueue)
                {
                    m_SendQueue[id].Enqueue(data);
                    m_SendDataAutoResetEvent.Set();
                }
            }
            catch (Exception e)
            {
                Logger.LogToTerminal(e.Message);
            }
        }
        public List<int> GetClientIDs()
        {
            return new List<int>(m_ClientsDic.Keys);
        }
        public void Receive()
        {
            IPEndPoint ipEndPoint=new IPEndPoint(IPAddress.Any, 0);
            byte[] data=m_Client.Receive(ref ipEndPoint);
            int id = -1;
            foreach (var VARIABLE in m_ClientsDic)
            {
                if (VARIABLE.Value.ip.Equals(ipEndPoint))
                {
                    id=VARIABLE.Key;
                    break;
                }
            }
            if (id == -1)
            {
                id = m_ClientNextID;
                m_ClientNextID++;
                m_ClientsDic.Add(id,new UDPClientInfo{id =id ,lastTime = DateTime.Now,ip = ipEndPoint});
                m_ClientsDic[id].SetID(id);
                m_ClientsDic[id].SetIPAddress(ipEndPoint);
                m_RecvQueue[id] = new Queue<byte[]>();
                m_SendQueue[id] = new Queue<byte[]>();
                Logger.LogToTerminal($"New Client, ID: {id}");
            }
            m_ClientsDic[id].SetLastTime();
            m_RecvQueue[id].Enqueue(data);
            OnReceive?.Invoke(id);
        }

        public List<byte[]> GetDataFromDic(int id)
        {
            List<byte[]> data = new List<byte[]>();
            while (m_RecvQueue[id].Count > 0)
            {
                data.Add(m_RecvQueue[id].Dequeue());
            }
            return data;
        }
    }
    
}