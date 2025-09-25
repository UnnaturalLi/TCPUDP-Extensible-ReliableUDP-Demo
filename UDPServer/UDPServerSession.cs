using System;
using System.Net.Sockets;
using System.Threading;
using NetworkBase;
using System.Collections.Generic;
using System.Linq;
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
        protected Dictionary<int, Queue<byte[]>> m_RecvQueue = new Dictionary<int, Queue<byte[]>>();
        protected Dictionary<int, Queue<byte[]>> m_SendQueue = new Dictionary<int, Queue<byte[]>>();
        protected Thread m_SendThread;
        public ReceiveData OnRegisterClient;
        public ReceiveData OnDropClient;
        protected byte[] m_HeartbeatPacket;
        protected int m_HeartbeatCheckTime;
        protected int m_HeartbeatDropTime;
        protected Dictionary<int, uint> m_Acks = new Dictionary<int, uint>();
        protected Dictionary<int, uint> m_Maps = new Dictionary<int, uint>();
        protected Dictionary<int, bool> m_AckUpdated = new Dictionary<int, bool>();
        protected Thread m_AckThread;

        public override bool OnInit(params object[] args)
        {

            OnReceive = (ReceiveData)args[0];
            OnRegisterClient = (ReceiveData)args[1];
            OnDropClient = (ReceiveData)args[2];
            m_ClientNextID = 0;
            m_HeartbeatPacket = (byte[])args[3];
            m_HeartbeatCheckTime = (int)args[4] * 1000;
            m_HeartbeatDropTime = (int)args[5] * 1000;
            try
            {
                m_Client = new UdpClient(args[6] as IPEndPoint);
                m_Client.Client.ReceiveTimeout = 20;
                return true;
            }
            catch (Exception e)
            {
                Logger.LogToTerminal("Server Init error: " + e);
            }

            return false;
        }

        public override void OnStart()
        {
            base.OnStart();
            m_SendThread = CreateThread(T_Send);
            m_AckThread = CreateThread(T_CheckACK);
        }

        public override void OnRun()
        {
            Receive();
            HearbeatCheck();
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
                lock (m_SendQueue)
                {
                    foreach (var VARIABLE in m_SendQueue)
                    {
                        if (VARIABLE.Value.Count > 0)
                        {
                            var data = VARIABLE.Value.Dequeue();
                            try
                            {
                                m_Client.Send(data, data.Length, m_ClientsDic[VARIABLE.Key].ip);
                            }
                            catch (Exception e)
                            {
                                Logger.LogToTerminal("Sending error: " + e);
                            }

                            sent = true;
                        }
                    }
                }

                if (!sent)
                {
                    m_SendDataAutoResetEvent.WaitOne(1000);
                }
            }
        }

        public void AppendToSend(int id, byte[] data)
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

        public void HearbeatCheck()
        {
            List<int> ClientsToDrop = null;
            foreach (var VARIABLE in m_ClientsDic)
            {
                var deltaTime = DateTime.Now - VARIABLE.Value.lastTime;
                if (deltaTime.TotalMilliseconds > m_HeartbeatDropTime)
                {
                    if (ClientsToDrop == null)
                    {
                        ClientsToDrop = new List<int>();
                    }

                    ClientsToDrop.Add(VARIABLE.Key);
                }
                else if (deltaTime.TotalMilliseconds > m_HeartbeatCheckTime)
                {
                    AppendToSend(VARIABLE.Key, m_HeartbeatPacket);
                }
            }

            if (ClientsToDrop != null)
            {
                foreach (var id in ClientsToDrop)
                {
                    lock (m_ClientsDic)
                    {
                        m_ClientsDic.Remove(id);
                    }

                    lock (m_RecvQueue)
                    {
                        m_RecvQueue.Remove(id);
                    }

                    lock (m_SendQueue)
                    {
                        m_SendQueue.Remove(id);
                    }

                    lock (m_Acks)
                    {
                        m_Acks.Remove(id);
                    }

                    lock (m_Maps)
                    {
                        m_Maps.Remove(id);
                    }

                    lock (m_AckUpdated)
                    {
                        m_AckUpdated.Remove(id);
                    }
                    OnDropClient?.Invoke(id);
                }
            }
        }

        public void Receive()
        {
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] data;
            try
            {
                data = m_Client.Receive(ref ipEndPoint);
            }
            catch (Exception e)
            {
                return;
            }

            int id = -1;
            foreach (var VARIABLE in m_ClientsDic)
            {
                if (VARIABLE.Value.ip.Equals(ipEndPoint))
                {
                    id = VARIABLE.Key;
                    break;
                }
            }

            if (id == -1)
            {
                id = m_ClientNextID;
                m_ClientNextID++;
                m_ClientsDic.Add(id, new UDPClientInfo { id = id, lastTime = DateTime.Now, ip = ipEndPoint });
                m_ClientsDic[id].SetID(id);
                m_ClientsDic[id].SetIPAddress(ipEndPoint);
                m_RecvQueue[id] = new Queue<byte[]>();
                m_SendQueue[id] = new Queue<byte[]>();
                Logger.LogToTerminal($"New Client, ID: {id}");
                OnRegisterClient?.Invoke(id);
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

        public void UpdateACK(uint ack, uint map, int id)
        {
            m_Acks[id] = ack;
            m_Maps[id] = map;
            m_AckUpdated[id] = true;
        }

        public void T_CheckACK()
        {
            while (true)
            {
                if (!m_IsRunning)
                {
                    Thread.Sleep(1000);
                    continue;
                }
                lock (m_AckUpdated)
                {
                    foreach (var pair in m_AckUpdated.ToList())
                    {
                        if (pair.Value == true)
                        {
                            byte[] packet = new byte[24];
                            Array.Copy(BitConverter.GetBytes(0u), 0, packet, 0, 4);
                            Array.Copy(BitConverter.GetBytes(-1), 0, packet, 4, 4);
                            Array.Copy(BitConverter.GetBytes(1), 0, packet, 8, 4);
                            Array.Copy(BitConverter.GetBytes(m_Acks[pair.Key]), 0, packet, 12, 4);
                            Array.Copy(BitConverter.GetBytes(0), 0, packet, 16, 4);
                            Array.Copy(BitConverter.GetBytes(m_Maps[pair.Key]), 0, packet, 20, 4);
                            AppendToSend(pair.Key, packet);
                            m_AckUpdated[pair.Key] = false;
                        }
                    }
                    
                }
                
                Thread.Sleep(1000);
            }
        }
    }
}