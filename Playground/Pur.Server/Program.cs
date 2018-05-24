using Bur.Net;
using Serilog;
using System;
using System.Net.Sockets;
using System.Threading;

namespace Pur.Server
{
    internal static class Program
    {
        private static void Main()
        {
            PurLogging.Configure("Server");

            var server = new NetServer(45685, AddressFamily.InterNetwork);

            var resetEvent = new ManualResetEvent(false);
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                Log.Information("Ctrl+C");

                server.Stop();
                resetEvent.Set();
            };

            server.Start();

            resetEvent.WaitOne();
            Thread.Sleep(1000);
        }
    }
}
