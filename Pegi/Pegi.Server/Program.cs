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

            var resetEvent = new ManualResetEvent(false);
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                Log.Information("Ctrl+C");

                server.Stop();
                resetEvent.Set();
            };


            server.Start();

            const int fps =  2;
            const int frame = 1000 / fps;
            var sw = Stopwatch.StartNew();
            var previous = sw.ElapsedMilliseconds;
            while (true)
            {
                var current = sw.ElapsedMilliseconds;
                var delta = current - previous;
                if (delta > frame)
                {
                    Console.WriteLine("ASD");

                    Thread.Sleep(frame);
                    previous = current;
                }
            }

            resetEvent.WaitOne();
            Thread.Sleep(1000);
        }
    }
}
