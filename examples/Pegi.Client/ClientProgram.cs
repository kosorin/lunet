using Lunet;
using Lunet.Builders;
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
            var builder = new ClientConnectionBuilder()
                .ConnectTo(new UdpEndPoint("127.0.0.1", 45685))
                .ConfigureChannels(builder => builder.AddChannel<ReliableOrderedChannel>(0))
                .UseLogger(PegiLogging.Configure("Client"))
                ;

            using (var connection = builder.Build())
            using (var resetEvent = new ManualResetEventSlim(false))
            {
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true;
                    Log.Information("Ctrl+C");
                    resetEvent.Set();
                };
                Thread.Sleep(200);

                connection.MessageReceived += (_, data) =>
                {
                    var message = MessagePackSerializer.Deserialize<DebugMessage>(data);
                    Log.Information("[{ConnectionEndPoint}] Message: {Message}", connection.RemoteEndPoint, message);
                };

                Log.Information("Connecting...");
                connection.Connect();
                Log.Information("Connected");

                var updateTime = 60;
                var sendTime = 4;
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
                    var delta = 1000 / sendTime;
                    if (now - time > delta)
                    {
                        time += delta;

                        for (var n = 0; n < 1; n++)
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
