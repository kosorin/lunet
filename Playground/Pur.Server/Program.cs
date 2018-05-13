using Bur.Net.Udp;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Pur.Server
{
    internal static class Program
    {
        private static void Main()
        {
            PurLogging.Initialize("Server");

            var addressFamily = AddressFamily.InterNetwork;
            var port = 45685;

            var server = NetPeer.CreateServer(addressFamily, port);
            server.Start();

            Thread.Sleep(200000);
            server.Stop();
        }
    }
}
