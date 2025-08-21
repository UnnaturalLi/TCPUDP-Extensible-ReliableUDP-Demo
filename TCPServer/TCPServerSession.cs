using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NetworkBase;
using System;
namespace TCPServer
{
    public class ClientInfo
    {
        public TcpClient client;
        public Thread receiveThread{get;set;}
        public int id { set; get; }
        public ClientInfo(TcpClient client)
        {
            this.client = client;
        }
    }
    public delegate void ReceiveData(int id);
    public class TCPServerSession : AppBase
    {
        protected Thread m_SendThread;
        protected List<ClientInfo> m_Clients = new List<ClientInfo>();
        protected List<Queue<byte[]>> m_RecvDataBuffers = new List<Queue<byte[]>>();
        protected List<Queue<byte[]>> m_SendDataBuffers = new List<Queue<byte[]>>();
        public ReceiveData OnReceive;
        protected AutoResetEvent m_SendDataAutoResetEvent = new AutoResetEvent(false);
        private TcpListener m_Listener;
        
       
        public override bool OnInit(params object[] args)
        {
            OnReceive = (ReceiveData) args[0];
            try
            {
                m_Listener = new TcpListener(args[1] as IPEndPoint);
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
            m_Listener.Start();
            m_SendThread = CreateThread(T_SendData);
        }

        public int GetClientCount()
        {
            return m_Clients.Count;
        }

        public byte[][] GetRecvDataBuffer(int i)
        {
            lock (m_RecvDataBuffers[i])
            {
                var data=m_RecvDataBuffers[i].ToArray();
                m_RecvDataBuffers[i].Clear();
                return data;
            }
        }
        public void T_ReceiveData(ClientInfo client)
        {
            NetworkStream stream = client.client.GetStream();
            byte[] buffer;
            while (m_IsRunning)
            {
                bool received = false;
                try
                {
                    if (client.client.Connected == false)
                    {
                        return;
                    }

                    while (stream.DataAvailable)
                    {
                        buffer = new byte[1024];
                        int n = stream.Read(buffer, 0, buffer.Length);
                        var chunk = new byte[n];
                        Buffer.BlockCopy(buffer, 0, chunk, 0, n);
                        lock (m_RecvDataBuffers)
                        {
                            m_RecvDataBuffers[client.id].Enqueue(chunk);
                            received = true;
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.LogToTerminal("error: " + e);
                }
                if(received){
                OnReceive?.Invoke(client.id);
                }
                Thread.Sleep(10);
            }
        }
//TODO:Remove
        public void T_SendData()
        {
            bool wrote = false;
            
            while (m_IsRunning)
            {
                    for (int i = 0; i < m_SendDataBuffers.Count; i++)
                    {
                        if (m_SendDataBuffers[i].Count == 0)
                        {
                            continue;
                        }
                        
                        var stream=m_Clients[i].client.GetStream();
                            try
                            {
                                byte[] data;
                                lock (m_SendDataBuffers)
                                {
                                     data = m_SendDataBuffers[i].Dequeue();
                                }
                                stream.Write(data, 0, data.Length);
                                wrote = true;
                            }
                            catch (Exception e)
                            {
                                Logger.LogToTerminal(e.ToString());
                            }
                   
                }
                if (!wrote)
                {
                    m_SendDataAutoResetEvent.WaitOne(1000);
                }
                else
                {
                    wrote = false;
                }
            }
        }

        public byte[][] GetData(int id)
        {
            byte[][] data;
            lock (m_RecvDataBuffers)
            {
                data = m_RecvDataBuffers[id].ToArray();
                m_RecvDataBuffers[id].Clear();
            }

            return data;
        }
        
        public void AppendToSend(int id,byte[] data)
        {
            lock (m_SendDataBuffers)
            {
                m_SendDataBuffers[id].Enqueue(data);
                m_SendDataAutoResetEvent.Set();
            }
        }
        public override void OnRun()
        {
            while (true)
            {
                if(!m_IsRunning){return;}
                try
                {
                    if(m_Listener.Pending())
                    {
                        TcpClient client = m_Listener.AcceptTcpClient();
                        lock (m_Clients)
                        {
                           
                            var clientInfo = new ClientInfo(client);
                            var thread = CreateThread(() => T_ReceiveData(clientInfo));
                            clientInfo.receiveThread = thread;
                            m_Clients.Add(clientInfo);
                            clientInfo.id = m_Clients.Count-1;
                            lock (m_RecvDataBuffers)
                            {
                                m_RecvDataBuffers.Add(new Queue<byte[]>());
                            }
                            lock (m_SendDataBuffers)
                            {
                                m_SendDataBuffers.Add(new Queue<byte[]>());
                            }
                            Logger.LogToTerminal($"Add client {client.Client.RemoteEndPoint}");
                        }
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                }
                catch (Exception e)
                {
                    Logger.LogToTerminal("error in accept thread:" + e);
                    return;
                }
            }
        }
    }
}