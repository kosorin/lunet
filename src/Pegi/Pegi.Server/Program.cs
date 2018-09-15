using Lure.Net;
using Lure.Net.Channels;
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
            var config = new NetServerConfiguration
            {
                ChannelFactory = channelFactory,
                LocalPort = 45685,
            };
            using (var server = new NetServer(config))
            {
                var resetEvent = new ManualResetEventSlim(false);
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true;
                    Log.Information("Ctrl+C");
                    resetEvent.Set();
                };

                server.MessageReceived += (connection, message) =>
                {
                    if (message != null && message is DebugMessage testMessage)
                    {
                        Log.Information("[{ConnectionEndPoint}] Message: {Message}", connection.RemoteEndPoint, message);
                        connection.SendMessage(new DebugMessage()
                        {
                            Integer = testMessage.Integer * 2,
                            Float = testMessage.Float * 2
                        });
                    }
                };
                server.Start();

                resetEvent.Wait();

                server.Stop();
            }

            Thread.Sleep(1000);
        }
    }
}
