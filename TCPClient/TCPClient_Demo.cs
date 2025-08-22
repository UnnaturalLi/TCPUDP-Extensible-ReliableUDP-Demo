using System;
using NetworkBase;

namespace TCPClient
{
    public class TCPClient_Demo: TCPClient
    {
        protected override void OnReceive()
        {
            while (dataQueue.Count > 0)
            {
                var packet = dataQueue.Dequeue();
                Logger.LogToTerminal($"Receive pack {(packet as Packet_Demo).x} {(packet as Packet_Demo).y}");
            }
        }
    }
}