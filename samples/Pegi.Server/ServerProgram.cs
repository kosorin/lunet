using Lunet;
using Lunet.Channels;
using Lunet.Data;
using Lunet.Messages;
using Lunet.Udp;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Pegi.Server
{
    internal static class ServerProgram
    {
        private static void Main()
        {
            PegiLogging.Configure("Server");

            var localEndPoint = new InternetEndPoint("127.0.0.1", 45685);

            var channelFactory = new DefaultChannelFactory();
            channelFactory.Add<ReliableOrderedChannel>();

            using (var listener = new UdpConnectionListener(localEndPoint, channelFactory))
            {
                var resetEvent = new ManualResetEventSlim(false);
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true;
                    Log.Information("Ctrl+C");
                    resetEvent.Set();
                };

                var connections = new ConcurrentDictionary<IEndPoint, IConnection>();

                listener.NewConnection += (_, connection) =>
                {
                    Log.Information("[{ConnectionEndPoint}] New connection", connection.RemoteEndPoint);
                    connection.Disconnected += (__) =>
                    {
                        Log.Information("[{ConnectionEndPoint}] Disconnected", connection.RemoteEndPoint);
                        connections.TryRemove(connection.RemoteEndPoint, out var ___);
                    };
                    connection.MessageReceived += (__, data) =>
                    {
                        var reader = new NetDataReader(data);
                        var typeId = reader.ReadUShort();
                        var message = NetMessageManager.Create(typeId);
                        if (message != null && message is DebugMessage testMessage)
                        {
                            message.DeserializeLib(reader);
                            Log.Information("[{ConnectionEndPoint}] Message: {Message}", connection.RemoteEndPoint, message);
                            var writer = new NetDataWriter();
                            writer.Reset();
                            message.SerializeLib(writer);
                            connection.SendMessage(writer.GetBytes());
                        }
                    };
                    connections.TryAdd(connection.RemoteEndPoint, connection);
                };
                listener.Start();

                var updateTime = 30;
                while (!resetEvent.IsSet)
                {
                    foreach (var connection in connections.Values)
                    {
                        connection.Update();
                    }
                    Thread.Sleep(1000 / updateTime);
                }

                Log.Information("Disconnecting...");
                foreach (var connection in connections.Values)
                {
                    connection.Disconnect();
                }
                Log.Information("Disconnected");
            }

            Thread.Sleep(1000);
        }
    }
}
