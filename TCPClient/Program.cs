using System;
using NetworkBase;
using System.Net;
namespace TCPClient
{
    internal class Program
    {
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
            client.SetReceivePacketIndex(0);
            client.Start();
            Console.WriteLine("\n---Press Return to Send---");
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true; 
                client.Stop();
                Logger.LogToTerminal("Client stopped.");
                Environment.Exit(0);
            };
            Packet_Demo packet = new Packet_Demo{x=1,y=2};
            while (true)
            {
                Console.ReadLine();
                client.Send(packet);
                Logger.LogToTerminal($"Packet {packet.x} {packet.y}sent.");
                packet.x += 1;
            }
        }
    }
}

