using Lure.Net;
using Serilog;
using System;
using System.Threading;

namespace Pegi.Server
{
    internal static class Program
    {
        private static void Main()
        {
            PegiLogging.Configure("Server");

            using (var server = new NetServer(45685))
            {
                var resetEvent = new ManualResetEventSlim(false);
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true;
                    Log.Information("Ctrl+C");
                    resetEvent.Set();
                };

                server.Start();

                resetEvent.Wait();

                server.Stop();
            }

            Thread.Sleep(1000);
        }
    }
}
