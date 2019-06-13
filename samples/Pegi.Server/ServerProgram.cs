using Lunet;
using Lunet.Channels;
using MessagePack;
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

            using (var listener = new ConnectionListener(localEndPoint, channelFactory))
            {
                var resetEvent = new ManualResetEventSlim(false);
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true;
                    Log.Information("Ctrl+C");
                    resetEvent.Set();
                };

                var connections = new ConcurrentDictionary<InternetEndPoint, Connection>();

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
                        var message = MessagePackSerializer.Deserialize<DebugMessage>(data);
                        Log.Information("[{ConnectionEndPoint}] Message: {Message}", connection.RemoteEndPoint, message);
                        connection.SendMessage(data);
                    };
                    connections.TryAdd(connection.RemoteEndPoint, connection);
                };
                listener.Run();

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
            }
            Log.Information("Disconnected");

            Thread.Sleep(1000);
        }
    }
}
