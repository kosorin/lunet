using Lure.Net;
using Lure.Net.Channels;
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

            var channelFactory = new NetChannelFactory();
            channelFactory.Add<ReliableOrderedChannel>();
            var config = new ClientPeerConfig
            {
                ChannelFactory = channelFactory,
                //Hostname = "bur.kosorin.net",
                Port = 45685,
                LocalPort = 45688,
            };
            using (var client = new ClientPeer(config))
            {
                var resetEvent = new ManualResetEventSlim(false);
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true;
                    Log.Information("Ctrl+C");
                    resetEvent.Set();
                };
                Thread.Sleep(1000);

                client.Start();

                var connection = client.Connection;
                connection.MessageReceived += (_, message) =>
                {
                    if (message != null && message is DebugMessage testMessage)
                    {
                        Log.Information("[{ConnectionEndPoint}] Message: {Message}", connection.RemoteEndPoint, message);
                    }
                };
                connection.Connect();

                var i = 0;
                while (!resetEvent.IsSet)
                {
                    client.Update();

                    i++;
                    var message = NetMessageManager.Create<DebugMessage>();
                    message.Integer = i;
                    message.Float = i;
                    connection.SendMessage(message);

                    Thread.Sleep(1000 / 50);
                }

                client.Stop();
            }

            Thread.Sleep(1000);
        }
    }
}
