using Lunet;
using Lunet.Channels;
using Lunet.Extensions;
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

            var remoteEndPoint = new UdpEndPoint("127.0.0.1", 45685);

            var channelSettings = new ChannelSettings();
            channelSettings.SetChannel(ChannelSettings.DefaultChannelId, (channelId, connection) => new UnreliableChannel(channelId, connection));

            using (var connection = new ClientConnection(remoteEndPoint, channelSettings))
            using (var resetEvent = new ManualResetEventSlim(false))
            {
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true;
                    Log.Information("Ctrl+C");
                    resetEvent.Set();
                };
                Thread.Sleep(500);

                connection.MessageReceived += (_, data) =>
                {
                    var message = MessagePackSerializer.Deserialize<DebugMessage>(data);
                    Log.Information("[{ConnectionEndPoint}] Message: {Message}", connection.RemoteEndPoint, message);
                };

                Log.Information("Connecting...");
                connection.Connect();
                Log.Information("Connected");

                var updateTime = 60;
                var sendTime = 10;
                var time = Timestamp.GetCurrent();
                var i = 0;
                while (!resetEvent.IsSet && connection.State == ConnectionState.Connected)
                {
                    try
                    {
                        connection.Update();
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Update error");
                        break;
                    }

                    var now = Timestamp.GetCurrent();
                    if (now - time > sendTime)
                    {
                        time += sendTime;

                        for (var n = 0; n < 10; n++)
                        {
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
