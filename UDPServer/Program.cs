using System;
using System.Net;
using NetworkBase;
using System.Threading;
namespace UDPServer
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            PacketFactoryBase factory = new Factory_Demo_UDP();
            factory.Init();
            UDPServer_Demo server = new UDPServer_Demo();
            if (!server.Init(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8880),0,5,10,60))
            {
                Logger.LogToTerminal("Failed to Init UDP server");
                return;
            }
            server.Start();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true; 
                server.Stop();
                Logger.LogToTerminal("Server stopped.");
                Environment.Exit(0);
            };
            Thread.Sleep(Timeout.Infinite);
        }
    }
}