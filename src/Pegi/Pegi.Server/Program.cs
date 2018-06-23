using Lure.Net;
using Serilog;
using System;
using System.Net.Sockets;
using System.Threading;

namespace Pegi.Server
{
    internal static class Program
    {
        private static void Main()
        {
            PegiLogging.Configure("Server");

            using (var server = new NetServer(45685, AddressFamily.InterNetwork))
            {
                var resetEvent = new ManualResetEvent(false);
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true;
                    Log.Information("Ctrl+C");
                    resetEvent.Set();
                };

                server.Start();

                resetEvent.WaitOne();

                server.Stop();
            }

            Thread.Sleep(1000);
        }
    }
}
