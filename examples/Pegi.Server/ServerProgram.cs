using System.Collections.Concurrent;
using Lunet;
using Lunet.Builders;
using Lunet.Channels;
using Lunet.Extensions;
using MessagePack;
using Serilog;

namespace Pegi.Server;

internal static class ServerProgram
{
    private static void Main()
    {
        var builder = new ConnectionListenerBuilder()
                .ListenOn(new UdpEndPoint("127.0.0.1", 45685))
                .ConfigureChannels(builder => builder.AddChannel<ReliableOrderedChannel>(0))
                .UseLogger(PegiLogging.Configure("Server"))
            ;

        using (var listener = builder.Build())
        using (var resetEvent = new ManualResetEventSlim(false))
        {
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                Log.Information("Ctrl+C");
                resetEvent.Set();
            };

            var connections = new ConcurrentDictionary<UdpEndPoint, Connection>();

            listener.NewConnection += (_, connection) =>
            {
                Log.Information("[{ConnectionEndPoint}] New connection", connection.RemoteEndPoint);
                connection.Disconnected += _ =>
                {
                    Log.Information("[{ConnectionEndPoint}] Disconnected", connection.RemoteEndPoint);
                    connections.TryRemove(connection.RemoteEndPoint, out _);
                };
                connection.MessageReceived += (_, data) =>
                {
                    var message = MessagePackSerializer.Deserialize<DebugMessage>(data);
                    Log.Information("[{ConnectionEndPoint}] Message: {Message}", connection.RemoteEndPoint, message);
                    connection.SendMessage(data);
                };
                connections.TryAdd(connection.RemoteEndPoint, connection);
            };
            listener.Run();

            var updateTime = 60;
            while (!resetEvent.IsSet)
            {
                foreach (var connection in connections.Values)
                {
                    try
                    {
                        connection.Update();
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Update error");
                        connection.Dispose();
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
