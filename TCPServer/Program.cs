using System.Net;
using NetworkBase;
using System;
using System.Threading;
namespace TCPServer
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Factory_Demo factory = new Factory_Demo();
            factory.Init();
            TCPServer_Demo server = new TCPServer_Demo();
            server.Init(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9888));
            server.SetReceivePacketIndex(0);
            server.Start();
            Logger.LogToTerminal("Server started. Press Ctrl+C to stop...");

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