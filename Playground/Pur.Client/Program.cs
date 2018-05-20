using Bur.Net;
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

            var client = new NetClient("localhost", 45685, AddressFamily.InterNetwork);
            client.Start();

            client.SendMessage("AHOJ!");
            Thread.Sleep(500);
            client.SendMessage("TESTIK 2");

            Thread.Sleep(100);

            client.Stop();
        }
    }
}
