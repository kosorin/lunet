using Bur.Game;
using Bur.Net.Server.Tcp;
using Bur.Net.Tcp;
using Serilog;
using System;
using System.Net;
using System.Threading;

namespace Bur
{
    internal static class Program
    {
        private static readonly ILogger logger = Log.ForContext(typeof(Program));

        private static void Main()
        {
            Logging.Initialize();

            var hostName = "127.0.0.1";
            var port = 45698;

            var ipAddress = IPAddress.Parse(hostName);
            var endPoint = new TcpEndPoint(ipAddress, port);
            var tcpServer = new TcpServer(endPoint);

            var cancellationTokenSource = new CancellationTokenSource();

            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                logger.Information("Cancel key press");
                cancellationTokenSource.Cancel();
            };

            var gameServer = new GameServer(tcpServer);
            try
            {
                gameServer.Run(cancellationTokenSource.Token);
            }
            catch (Exception e)
            {
                logger.Fatal(e, "Unhandled exception");
            }
        }
    }
}
