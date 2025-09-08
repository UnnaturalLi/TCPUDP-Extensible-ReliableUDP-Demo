# Network_Base_For_UDP_And_TCP
A simple and extensible networking demo for TCP & UDP in C#. Both server and client can send and receive different message types via user-defined classes implementing a shared interface.
# Update:
UDP now supports reliable transfer using seq and ack,please check it out in sub branch  
## ✨ Features
- TCP + UDP examples with a unified structure
- Extensible message system: implement `INetPacket` and register in the factory to customize send/receive functions
- Unified packet header:  
  UDP(None Reliable):[ Length(4) ][ TypeId(4) ][ Payload(Length) ]  
  UDP(Reliable):[ Length(4) ][ TypeId(4) ][ Flag(4) ][ Ack(4) ][ Seq(4) ][ AckMap(4) ][ Payload(Length) ]  
  TCP:[ Length(4) ][ Payload(Length) ]
- Length: payload size (not including header)
- TypeId: message type id
- Payload: serialized object data
- TCP: handles sticky/fragmented packets via ring buffer, supports multiple packets in one stream
- UDP: keeps datagram boundaries, simple demo included
- Queue-based dispatch: receiving thread parses and enqueues packets, user logic consumes them

## 🛠️ Extending with Your Own Packet
Implement the interface:  
```csharp
public class RTTPacket_Demo : INetPacket  
    {
        public DateTime sentTime;  
        public byte[] ToBytes()  
        {  
            return BitConverter.GetBytes(sentTime.Ticks);  
        }  
        public void FromBytes(byte[] data)  
        {  
            sentTime=new DateTime(BitConverter.ToInt64(data, 0));  
        }  
    }
```
Register in factory:
```csharp
public class Factory_Demo: PacketFactoryBase
    {
        protected override bool OnInit()
        {
            PacketTypeDic.Add(0,typeof(Packet_Demo));
            PacketTypeDic.Add(1,typeof(RTTPacket_Demo));
            PacketTypeDic.Add(2,typeof(HeartbeatPacket_Demo));
            return true;
        }
    }
```
## 🚀 Customize For Server\Client Behaviour
Send / Receive:
For Client(Same to TCP and UDP)
```csharp
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
```
For Server(Same to TCP and UDP)
```csharp
public class TCPServer_Demo : TCPServer
    {
        public override void OnReceiveObj(int id, INetPacket obj)
        {
            Logger.LogToTerminal($"Receive from clent{id} : {(obj as Packet_Demo).x} {(obj as Packet_Demo).y}");
            SendTo(id,obj);
        }
    }
```
## 🚀 Build & Run
Default programs run local demo code. Change IP/port in Program.cs as needed.  
```csharp
/*
To run the client or Server, you should:
1. Initialize the packet factory.
   - Register all your custom packet types inside Factory.OnInit().
   - This allows the framework to map TypeId ↔ Packet class.

3. Create and initialize the Client or Server.
   - Init(endpoint) will attempt to connect to the server.
   - If it fails, check IP, port, or firewall.

4. For TCP transfer, Set packet ID (Skip for UDP)
   - SetReceivePacketIndex(id) will attempt to set the packet ID to the Server/Client

5. Make sure the Server/Client is started after initialization
   - Start() will start the Server/Client

6. Add your own application logic below Start().
   - For example, sending custom packets
*/
public static void Main(string[] args)
        {
            Factory_Demo factory_Demo = new Factory_Demo();
            if (!factory_Demo.Init())
            {
                Logger.LogToTerminal("Fall to init Factory");
                return;
            }
            TCPClient_Demo client = new TCPClient_Demo();
            if (!client.Init(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9888)))
            {
                Logger.LogToTerminal("Fall to connect to the server");
                return;
            }
            client.SetReceivePacketIndex(0); //to use TCP, please set the packet index(refer to the factory)
            client.Start();
            //customize your logic below
        }
```

## 📡 Protocol Notes  
Header format  
UDP(None Reliable):[ Length(4) ][ TypeId(4) ][ Payload(Length) ]  
UDP(Reliable):[ Length(4) ][ TypeId(4) ][ Flag(4) ][ Ack(4) ][ Seq(4) ][ AckMap(4) ][ Payload(Length) ]  
TCP:[ Length(4 bytes, uint) ]  
Receiving reads length (and type Id), uses factory to create object and FromBytes  
Current demo uses little-endian (default of BitConverter).  
For cross-platform compatibility, consider switching to network byte order (big-endian).  
**TCP**  
Stream based, manual packet splitting/merging  
Uses ring buffer to handle multiple packets  
**UDP**  
Each packet ≤ 1200 bytes recommended to avoid fragmentation  
Demo does not implement reliability layer

## 📜 License
MIT License  
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
