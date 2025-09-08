using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using NetworkBase;
using NetworkBase;
namespace TCPClient
{
    public abstract class TCPClient : ClientBase 
    {
        public Queue<INetPacket> dataQueue;
        public bool isRunning;
        private byte[] m_recvBuffer;
        private uint m_recvBufferSize;
        private uint _ReadPos=0, _WritePos=0;
        protected int m_RecevPacketIndex;
        protected override bool OnInit(params object[] args)
        {
            dataQueue = new Queue<INetPacket>();
            m_recvBuffer = new byte[1024];
            session = new TCPSession();
            (session as TCPSession).OnDataReceived += Receive;
            if (!session.Init(m_Addr))
            {
                return false;
            }
            base.OnInit();
            return true;
        }

        public void SetReceivePacketIndex(int index)
        {
            m_RecevPacketIndex = index;
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
            var data = packet.ToBytes();
            byte[] sendBuffer=new byte[data.Length + 4];
            Array.Copy(BitConverter.GetBytes(data.Length), 0, sendBuffer, 0, 4);
            Array.Copy(data, 0, sendBuffer, 4, data.Length);
            (session as TCPSession)?.AppendToSendQueue(sendBuffer);
        }

        protected abstract void OnReceive();
        private void Receive()
        {
            var data = (session as TCPSession).GetReceivedData();
                for (int i = 0; i < data.Length; i++)
                {
                    for (int j = 0; j < data[i].Length; j++)
                    {
                        m_recvBuffer[_WritePos%1024] = data[i][j];
                        _WritePos++;
                        _WritePos %= 1024;
                    }
                }
                while ((_WritePos - _ReadPos + 1024) % 1024 >= 4)
                {
                    byte[] headBuffer = new byte[4];
                    for (int j = 0; j < 4; j++)
                    {
                        headBuffer[j] = m_recvBuffer[(_ReadPos + j) % 1024];
                    }
                    uint length = BitConverter.ToUInt32(headBuffer, 0);
                    if (length == 0)
                    {
                        _ReadPos += 4;
                        _ReadPos %= 1024;
                        continue;
                    }
                    if (length + 4 > (_WritePos - _ReadPos + 1024) % 1024)
                    {
                        break;
                    }

                    _ReadPos += 4;
                    _ReadPos %= 1024;
                    byte[] packetBuffer = new byte[length];
                    for (int j = 0; j < length; j++)
                    {
                        packetBuffer[j] = m_recvBuffer[_ReadPos % 1024];
                        _ReadPos++;
                        _ReadPos %= 1024;
                    }
                    INetPacket obj = PacketFactoryBase.Instance.GetPacket(m_RecevPacketIndex);
                    if (obj == null)
                    {
                        throw new Exception("Factory Error");
                    }
                    obj.FromBytes(packetBuffer);
                    lock (dataQueue)
                    {
                        dataQueue.Enqueue(obj);
                    }
                   
                    OnReceive();
                }
        }
    }
}