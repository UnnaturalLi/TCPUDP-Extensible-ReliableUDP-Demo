using NetworkBase;
using System;
namespace UDPServer
{
    public class UDPServer_Demo : UDPServer
    {
        public override void OnReceiveObj(int id, INetPacket obj)
        {
            if (obj.GetType() == typeof(HeartbeatPacket_Demo))
            {
                
            }
            else if (obj.GetType() == typeof(RTTPacket_Demo))
            {
                SendTo(id,obj);
            }
            else if (obj.GetType() == typeof(Packet_Demo))
            {
                Logger.LogToTerminal($"Received from {id} : {(obj as Packet_Demo).x} {(obj as Packet_Demo).y}");
                SendTo(id,obj);
            }
        }

        public override void OnRegisterClient(int id)
        {
            Logger.LogToTerminal($"Register client {id}");
        }

        public override void OnDropClient(int id)
        {
            Logger.LogToTerminal($"Dropped client {id}");
        }
    }
}