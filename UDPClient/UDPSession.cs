using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using TCPClient;
using System;
using System.Net;

namespace UDPClient

{
    using NetworkBase;
    public class UDPSession : NetworkSessionBase
    {
        public DataReceivedHandler OnDataReceived;
        protected UdpClient m_UdpClient;
        protected Thread m_SendThread;
        protected Thread m_ReceiveThread;
        private bool m_IsRunning;
        private Queue<byte[]> m_SendBuffers = new Queue<byte[]>();
        private readonly AutoResetEvent m_HasDataToSend = new AutoResetEvent(false);
        
        private Queue<byte[]> m_ReceiveBuffers = new Queue<byte[]>();
        public readonly AutoResetEvent hasDataToHandle = new AutoResetEvent(false);
        
        private uint m_Ack;
        private uint m_AckMap;
        private bool m_AckUpdated;
        private Thread m_AckThread;
        protected override bool OnInit()
        {
            try
            {
                m_UdpClient = new UdpClient();
                m_UdpClient.Connect(m_Addr);
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
            m_AckThread = CreateThread(T_SendAck);
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
            if (m_UdpClient != null)
            {
                m_UdpClient.Close();
                m_UdpClient = null;
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
            byte[] buffer;
            while (true)
            {
                if(!m_IsRunning|| isClosed){return;}
                try
                {
                    bool enqueue = false;
                    while (m_UdpClient.Available > 0)
                    {
                        byte[] data=m_UdpClient.Receive(ref m_Addr);
                        lock (m_ReceiveBuffers)
                        {
                            m_ReceiveBuffers.Enqueue(data);
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
            Byte[][] data;
            while (m_IsRunning)
            {
                try
                {
                    if (isClosed)
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
                        m_UdpClient.Send(data[i], data[i].Length);
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
        public void UpdateAck(uint ack, uint ackMap)
        {
            m_Ack = ack;
            m_AckMap = ackMap;
            m_AckUpdated=true;
        }

        public void T_SendAck()
        {
            while(true){
                if (!m_IsRunning|| m_AckUpdated==false)
                {
                    Thread.Sleep(1000);
                    continue;
                }
                byte[] packet = new byte[24];
                Array.Copy(BitConverter.GetBytes(0u),0,packet,0,4);
                Array.Copy(BitConverter.GetBytes(-1),0,packet,4,4);
                Array.Copy(BitConverter.GetBytes(1),0,packet,8,4);
                Array.Copy(BitConverter.GetBytes(m_Ack),0,packet,12,4);
                Array.Copy(BitConverter.GetBytes(0),0,packet,16,4);
                Array.Copy(BitConverter.GetBytes(m_AckMap),0,packet,20,4);
                AppendToSendQueue(packet);
                m_AckUpdated=false;
                Thread.Sleep(1000);
            }
        }
    }
    
}