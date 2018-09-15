using Lure.Net;
using Lure.Net.Channels;
using Lure.Net.Messages;
using Serilog;
using System;
using System.Net.Sockets;
using System.Threading;

namespace Pegi.Client
{
    internal static class Program
    {
        private static void Main()
        {
            PegiLogging.Configure("Client");

            var channelFactory = new NetChannelFactory();
            channelFactory.Add<ReliableOrderedChannel>();
            var config = new NetClientConfiguration
            {
                ChannelFactory = channelFactory,
                Hostname = "localhost",
                Port = 45685,
                LocalPort = 45688,
            };
            using (var client = new NetClient(config))
            {
                var resetEvent = new ManualResetEventSlim(false);
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true;
                    Log.Information("Ctrl+C");
                    resetEvent.Set();
                };

                client.MessageReceived += (connection, message) =>
                {
                    if (message != null && message is DebugMessage testMessage)
                    {
                        Log.Information("[{ConnectionEndPoint}] Message: {Message}", connection.RemoteEndPoint, message);
                    }
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
                    client.ServerConnection.SendMessage(message);

                    Thread.Sleep(1000 / 50);
                }

                client.Stop();
            }

            Thread.Sleep(1000);
        }
    }
}
