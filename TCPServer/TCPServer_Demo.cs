using System;
using NetworkBase;

namespace TCPServer
{
    public class TCPServer_Demo : TCPServer<Packet_Demo> 
    {
        public override void OnReceiveObj(int id, Packet_Demo obj)
        {
            Logger.LogToTerminal($"Receive from {id} : {obj.x} {obj.y}");
            SendTo(id,obj);
        }
    }
}