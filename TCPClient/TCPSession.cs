using System.Net.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using NetworkBase;
namespace TCPClient
{
    public class TCPSession : NetworkSessionBase
    {
        public delegate void DataReceivedHandler();
        public DataReceivedHandler OnDataReceived;
        protected TcpClient m_TcpClient;
        protected Thread m_SendThread;
        protected Thread m_ReceiveThread;
        private bool m_IsRunning;
        private Queue<byte[]> m_SendBuffers = new Queue<byte[]>();
        private readonly AutoResetEvent m_HasDataToSend = new AutoResetEvent(false);
        
        private Queue<byte[]> m_ReceiveBuffers = new Queue<byte[]>();
        public readonly AutoResetEvent hasDataToHandle = new AutoResetEvent(false);
        
        protected override bool OnInit()
        {
            try
            {
                m_TcpClient = new TcpClient();
                m_TcpClient.Connect(m_Addr);
                return true;
            }
            catch (Exception e)
            {
                Logger.LogToTerminal("Init error: " + e.ToString());
                return false;
            }

            
        }

        protected override void OnStart()
        {
            m_IsRunning = true;
            m_SendThread = CreateThread(T_Send);
            m_ReceiveThread= CreateThread(T_Receive);
        }

        protected override void OnClose()
        {
            m_IsRunning = false;
            if (m_SendThread != null)
            {
                m_SendThread.Join(3000);
                m_SendThread = null;
            }
            if (m_ReceiveThread != null)
            {
                m_ReceiveThread.Join(3000);
                m_ReceiveThread = null;
            }
            if (m_TcpClient != null)
            {
                m_TcpClient.Close();
                m_TcpClient = null;
            }
            if (hasDataToHandle != null)
            {
                hasDataToHandle.Dispose();
            }
            if (m_HasDataToSend != null)
            {
                m_HasDataToSend.Dispose();
            }
        }
        public byte[][] GetReceivedData()
        {
            byte[][] data = null;
            lock (m_ReceiveBuffers)
            {
                 data = m_ReceiveBuffers.ToArray();
                m_ReceiveBuffers.Clear();
            }
            return data;
        }
        public bool AppendToSendQueue(byte[] data)
        {
            lock (m_SendBuffers)
            {
                m_SendBuffers.Enqueue(data);
            }

            m_HasDataToSend.Set();
            return true;
        }

        protected void T_Receive()
        {
            var networkStream = m_TcpClient.GetStream();
            byte[] buffer;
            while (true)
            {
                if(!m_IsRunning){return;}
                    try
                    {
                        if (isClosed || m_TcpClient.Connected == false)
                        {
                            return;
                        }

                        bool enqueue = false;
                        while (networkStream.DataAvailable)
                        {
                            buffer = new byte[1024];
                            int n = networkStream.Read(buffer, 0, buffer.Length);
                            var chunk = new byte[n];
                            Buffer.BlockCopy(buffer, 0, chunk, 0, n);
                            lock (m_ReceiveBuffers)
                            {
                                m_ReceiveBuffers.Enqueue(chunk);
                                enqueue = true;
                            }
                        }

                        if (enqueue)
                        {
                            OnDataReceived?.Invoke();
                        }

                        Thread.Sleep(100);
                    }
                    catch (Exception e)
                    {
                        Logger.LogToTerminal("Receive error: " + e.ToString());
                    }
            }
        }

        protected void T_Send()
        {
            var networkStream = m_TcpClient.GetStream();
            Byte[][] data;
            while (m_IsRunning)
            {
                try
                {
                    if (isClosed || m_TcpClient.Connected == false)
                    {
                        return;
                    }
                    Queue<byte[]> buffer = new Queue<byte[]>();
                    lock (m_SendBuffers)
                    {
                        while (m_SendBuffers.Count > 0)
                        {
                            buffer.Enqueue(m_SendBuffers.Dequeue());
                        }
                    }
                    data= buffer.ToArray();
                    for (int i = 0; i < data.Length; i++)
                    {
                        networkStream.Write(data[i], 0, data[i].Length);
                    }
                    data = null;
                }
                catch (Exception e)
                {
                    Logger.LogToTerminal("Send error: " + e.ToString());
                }

                m_HasDataToSend.WaitOne(1000);
            }
        }
    }
}