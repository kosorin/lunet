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
                var resetEvent = new ManualResetEventSlim(false);
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true;
                    Log.Information("Ctrl+C");
                    resetEvent.Set();
                };

                client.Start();

                Thread.Sleep(500);

                for (int i = 0; ; i++)
                {
                    if (resetEvent.IsSet)
                    {
                        break;
                    }

                    var message = NetMessageManager.Create<DebugMessage>();
                    message.Integer = i;
                    message.Float = i * 3;
                    client.Connection.SendMessage(1, message);

                    Thread.Sleep(1000 / 50);
                }

                client.Stop();
            }

            Thread.Sleep(1000);
        }
    }
}
