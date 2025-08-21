using System;
using NetworkBase;

namespace TCPClient
{
    public class TCPClient_Demo: TCPClient<Packet_Demo>
    {
        protected override void OnReceive()
        {
            while (dataQueue.Count > 0)
            {
                var packet = dataQueue.Dequeue();
                Logger.LogToTerminal($"Receive pack {packet.x} {packet.y}");
            }
        }
    }
}