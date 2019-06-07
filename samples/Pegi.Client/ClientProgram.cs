using Lunet;
using Lunet.Channels;
using MessagePack;
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

            var remoteEndPoint = new InternetEndPoint("127.0.0.1", 45685);

            var channelFactory = new DefaultChannelFactory();
            channelFactory.Add<ReliableOrderedChannel>();

            using (var connection = new ClientConnection(remoteEndPoint, channelFactory))
            {
                var resetEvent = new ManualResetEventSlim(false);
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true;
                    Log.Information("Ctrl+C");
                    resetEvent.Set();
                };
                Thread.Sleep(1000);

                connection.MessageReceived += (_, data) =>
                {
                    var message = MessagePackSerializer.Deserialize<DebugMessage>(data);
                    Log.Information("[{ConnectionEndPoint}] Message: {Message}", connection.RemoteEndPoint, message);
                };

                connection.Connect();

                var updateTime = 30;
                var sendTime = 30;
                var time = Timestamp.Current;
                var i = 0;
                while (!resetEvent.IsSet && (connection.State == ConnectionState.Connecting || connection.State == ConnectionState.Connected))
                {
                    connection.Update();

                    var now = Timestamp.Current;
                    if (now - time > sendTime)
                    {
                        time += sendTime;

                        var message = new DebugMessage
                        {
                            Id = i,
                            Text = $"Zpráva {i}",
                        };
                        var messageBytes = MessagePackSerializer.Serialize(message);

                        connection.SendMessage(messageBytes);

                        i++;
                        if (i == 100_000)
                        {
                            break;
                        }
                    }

                    Thread.Sleep(1000 / updateTime);
                }

                Log.Information("Disconnecting...");
            }
            Log.Information("Disconnected");

            Thread.Sleep(1000);
        }
    }
}
