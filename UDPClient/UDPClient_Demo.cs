using System;
using NetworkBase;

namespace UDPClient
{
    public class UDPClient_Demo: UDPClient
    {
        protected override void OnReceive()
        {
            lock (dataQueue)
            {
                while (dataQueue.Count > 0)
                {
                    
                    var data = dataQueue.Dequeue();
                    
                    if (data.GetType() == typeof(HeartbeatPacket_Demo))
                    {
                        Send(data);
                    }
                    else if (data.GetType() == typeof(RTTPacket_Demo))
                    {
                        var deltaTime = DateTime.Now - (data as RTTPacket_Demo).sentTime;
                        Logger.LogToTerminal($"RTT: {deltaTime.Milliseconds}");
                    }
                    else if (data.GetType() == typeof(Packet_Demo))
                    {
                        Logger.LogToTerminal($"Received: {(data as Packet_Demo).x} {(data as Packet_Demo).y}");
                    }
                }
            }
        }
    }
}