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
                var resetEvent = new AutoResetEvent(false);
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true;
                    Log.Information("Ctrl+C");

                    client.Stop();
                    resetEvent.Set();
                };

                client.Start();

                Thread.Sleep(500);

                for (int i = 0; i < 10; i++)
                {
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
