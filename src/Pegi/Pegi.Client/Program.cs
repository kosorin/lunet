using Lure.Net;
using Lure.Net.Messages;
using Serilog;
using System;
using System.Threading;

namespace Pegi.Client
{
    internal static class Program
    {
        private static void Main()
        {
            PegiLogging.Configure("Client");

            using (var client = new NetClient("localhost", 45685))
            {
                var resetEvent = new ManualResetEvent(false);
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true;
                    Log.Information("Ctrl+C");
                    resetEvent.Set();
                };

                client.Start();

                Thread.Sleep(500);

                for (int i = 0; i < 100; i++)
                {
                    if (resetEvent.WaitOne(0))
                    {
                        break;
                    }

                    var message = new TestMessage
                    {
                        Integer = i * 10,
                        Float = i * 1.5f,
                    };
                    client.SendMessage(message);
                    Thread.Sleep(500);
                }

                resetEvent.WaitOne();

                client.Stop();
            }

            Thread.Sleep(1000);
        }
    }
}
