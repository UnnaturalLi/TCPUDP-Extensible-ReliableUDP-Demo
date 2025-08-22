using System;
using NetworkBase;

namespace TCPServer
{
    public class TCPServer_Demo : TCPServer
    {
        public override void OnReceiveObj(int id, INetPacket obj)
        {
            Logger.LogToTerminal($"Receive from clent{id} : {(obj as Packet_Demo).x} {(obj as Packet_Demo).y}");
            SendTo(id,obj);
        }
    }
}