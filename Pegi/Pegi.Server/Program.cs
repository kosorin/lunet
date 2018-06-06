using Lure.Net;
using Serilog;
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;

namespace Pegi.Server
{
    internal static class Program
    {
        private static void Main()
        {
            PegiLogging.Configure("Server");

            var server = new NetServer(45685, AddressFamily.InterNetwork);

            var resetEvent = new AutoResetEvent(false);
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
