using Lure;
using Lure.Net;
using Lure.Net.Channels;
using Lure.Net.Data;
using Lure.Net.Messages;
using Lure.Net.Tcp;
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
            channelFactory.Add<SimpleChannel>();

            var config = new ClientConfiguration
            {
                //Hostname = "bur.kosorin.net",
                Port = 45685,
                LocalPort = 45688,
            };

            using (var connection = new TcpConnection(remoteEndPoint, channelFactory, config))
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
                    var reader = new NetDataReader(data);
                    var typeId = reader.ReadUShort();
                    var message = NetMessageManager.Create(typeId);
                    if (message != null && message is DebugMessage testMessage)
                    {
                        message.DeserializeLib(reader);
                        Log.Information("[{ConnectionEndPoint}] Message: {Message}", connection.RemoteEndPoint, message);
                    }
                };

                connection.Connect();

                var writer = new NetDataWriter();
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

                Log.Information("Disconnecting...");
            }
            Log.Information("Disconnected");

            Thread.Sleep(1000);
        }
    }
}
