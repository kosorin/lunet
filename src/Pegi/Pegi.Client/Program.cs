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

                client.Connect();

                Thread.Sleep(500);

                for (int i = 0; i <= 1_000_000; i++)
                {
                    if (resetEvent.Wait(0))
                    {
                        break;
                    }

                    if (client.IsRunning && client.Connection.State == NetConnectionState.Connected)
                    {
                        var message = NetMessageManager.Create<DebugMessage>();
                        message.Integer = i;
                        message.Float = i * 3;
                        client.Connection.SendMessage(message);
                        Thread.Sleep(1000 / 50);

                        if (i == 200)
                        {
                            client.Disconnect();
                        }
                    }
                }

                resetEvent.Wait();

                client.Disconnect();
            }

            Thread.Sleep(1000);
        }
    }
}
