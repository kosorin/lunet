using Bur.Net;
using Bur.Net.Server;
using Serilog;
using System.Text;
using System.Threading;

namespace Bur.Game
{
    public class GameServer
    {
        private static readonly ILogger logger = Log.ForContext<GameServer>();

        private readonly INetServer netServer;

        public GameServer(INetServer networkServer)
        {
            this.netServer = networkServer;
        }

        public void Run(CancellationToken cancellationToken)
        {
            Start();

            cancellationToken.WaitHandle.WaitOne();

            Stop();
        }

        private void Start()
        {
            netServer.ClientConnected += NetServer_ClientConnected;
            netServer.ClientDisconnected += NetServer_ClientDisconnected;
            netServer.Start();
        }

        private void Stop()
        {
            netServer.Stop();
        }

        private void NetServer_ClientConnected(INetServer netServer, NetClientConnectedEventArgs e)
        {
            var netClient = e.Client;
            logger.Information("[{ClientId}] Client {ClientRemoteEndPoint} connected", netClient.Id, netClient.RemoteEndPoint);
            netClient.DataReceived += NetClient_DataReceived;
        }

        private void NetServer_ClientDisconnected(INetServer netServer, NetClientDisconnectedEventArgs e)
        {
            var netClient = e.Client;
            logger.Information("[{ClientId}] Client disconnected", netClient.Id);
            netClient.DataReceived -= NetClient_DataReceived;
        }

        private void NetClient_DataReceived(INetClient netClient, DataReceivedEventArgs e)
        {
            var encoding = Encoding.UTF8;
            var message = encoding.GetString(e.Data);
            logger.Information("[{ClientId}] Message ({MessageLength}): {Message}", netClient.Id, message.Length, message);
        }
    }
}
