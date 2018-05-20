using Bur.Net;
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

            var server = new NetServer(45685, AddressFamily.InterNetwork);
            server.Start();

            Thread.Sleep(2000);

            server.Stop();
        }
    }
}
