using System;
using System.Net;
using NetworkBase;
namespace UDPClient
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            PacketFactoryBase factory = new Factory_Demo_UDP();
            factory.Init();
            UDPClient_Demo udpClient = new UDPClient_Demo();
            if (!udpClient.Init(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8880),0))
            {
                Logger.LogToTerminal("Failed to connect to UDP server");
                return;
            }
            udpClient.Start();
            Console.WriteLine("UDP Client Started");
            Console.WriteLine("-------------------");
            Console.WriteLine("1 to Send RTT Packet");
            Console.WriteLine("2 to Send Demo Packet");
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true; 
                udpClient.Stop();
                Logger.LogToTerminal("Client stopped.");
                Environment.Exit(0);
            };
            var packet = new Packet_Demo { x = 0, y = 1 };
            while (true)
            {
                var cmd=Console.ReadLine();
                if (cmd.Equals("1"))
                {
                    udpClient.Send(new RTTPacket_Demo{sentTime = DateTime.Now});
                }else if (cmd.Equals("2"))
                {
                    udpClient.Send(packet);
                    packet.x++;
                }
            }
        }
    }
}