using Lure.Net;
using Lure.Net.Channels;
using Lure.Net.Channels.Message;
using Lure.Net.Messages;
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

            var channelFactory = new NetChannelFactory();
            channelFactory.Add<ReliableOrderedChannel>();
            var config = new ServerPeerConfig
            {
                ChannelFactory = channelFactory,
                LocalPort = 45685,
            };
            using (var server = new ServerPeer(config))
            {
                var resetEvent = new ManualResetEventSlim(false);
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true;
                    Log.Information("Ctrl+C");
                    resetEvent.Set();
                };

                server.NewConnection += (_, connection) =>
                {
                    connection.MessageReceived += (__, message) =>
                    {
                        if (message != null && message is DebugMessage testMessage)
                        {
                            Log.Information("[{ConnectionEndPoint}] Message: {Message}", connection.RemoteEndPoint, message);
                            testMessage.Integer *= 2;
                            testMessage.Float *= 2;
                            connection.SendMessage(testMessage);
                        }
                    };
                };
                server.Start();

                var updateTime = 30;
                while (!resetEvent.IsSet)
                {
                    server.Update();
                    Thread.Sleep(1000 / updateTime);
                }

                server.Stop();
            }

            Thread.Sleep(1000);
        }
    }
}
