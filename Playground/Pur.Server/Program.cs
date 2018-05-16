using Bur.Net.Udp;
using Serilog;
using System.Net.Sockets;
using System.Threading;

namespace Pur.Server
{
    internal static class Program
    {
        private static readonly ILogger logger = Log.ForContext(typeof(Program));

        private static void Main()
        {
            PurLogging.Initialize("Server");

            var addressFamily = AddressFamily.InterNetwork;
            var port = 45685;

            var server = NetConnection.CreateServer(addressFamily, port);
            server.Start();

            Thread.Sleep(200000);
            server.Stop();
        }
    }
}
