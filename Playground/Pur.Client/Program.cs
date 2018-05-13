using Bur.Net.Udp;
using System.Net.Sockets;
using System.Threading;

namespace Pur.Client
{
    internal static class Program
    {
        private static void Main()
        {
            PurLogging.Initialize("Client");

            Thread.Sleep(1000);

            var addressFamily = AddressFamily.InterNetwork;
            var hostName = "localhost";
            var port = 45685;

            var client = NetPeer.CreateClient(addressFamily, hostName, port);
            client.Start();

            client.EnqueueMessage("AHOJ!");
            Thread.Sleep(500);
            client.EnqueueMessage("TESTIK 2");

            Thread.Sleep(100000);
            client.Stop();
        }
    }
}
