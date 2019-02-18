using Lure;
using Lure.Net;
using Lure.Net.Channels;
using Lure.Net.Data;
using Lure.Net.Messages;
using Serilog;
using System;
using System.Threading;

namespace Pegi.Client
{
    internal static class ClientProgram
    {
        private static void Main()
        {
            PegiLogging.Configure("Client");

            var channelFactory = new ChannelFactory();
            channelFactory.Add<ReliableOrderedChannel>();
            var config = new ClientConfiguration
            {
                //Hostname = "bur.kosorin.net",
                Port = 45685,
                LocalPort = 45688,
            };
            using (var client = new ClientPeer(config, channelFactory))
            {
                var resetEvent = new ManualResetEventSlim(false);
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true;
                    Log.Information("Ctrl+C");
                    resetEvent.Set();
                };
                Thread.Sleep(1000);

                var connection = client.Connection;
                connection.MessageReceived += (_, message) =>
                {
                    //if (message != null && message is DebugMessage testMessage)
                    //{
                    //    Log.Information("[{ConnectionEndPoint}] Message: {Message}", connection.RemoteEndPoint, message);
                    //}
                };

                client.Start();

                var writer = new NetDataWriter();
                var updateTime = 30;
                var sendTime = 30;
                var time = Timestamp.Current;
                var i = 0;
                while (!resetEvent.IsSet && (connection.State == ConnectionState.Connecting || connection.State == ConnectionState.Connected))
                {
                    client.Update();

                    var now = Timestamp.Current;
                    if (now - time > sendTime)
                    {
                        time += sendTime;

                        var message = NetMessageManager.Create<DebugMessage>();
                        message.Integer = i;
                        message.Float = i;

                        writer.Reset();
                        message.SerializeLib(writer);
                        connection.SendMessage(writer.GetBytes());

                        i++;
                        if (i == 1000)
                        {
                            break;
                        }
                    }

                    Thread.Sleep(1000 / updateTime);
                }

                client.Stop();
            }

            Thread.Sleep(1000);
        }
    }
}
