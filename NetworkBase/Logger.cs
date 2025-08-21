using System;

namespace NetworkBase
{
    public static class Logger
    {
        public static void LogToTerminal(string msg)
        {
            Console.WriteLine($"{DateTime.Now}: {msg}");
        }
    }
}