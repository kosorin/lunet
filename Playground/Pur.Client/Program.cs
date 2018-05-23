using Bur.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Pur.Client
{
    internal static class Program
    {
        private static void Main()
        {
            PurLogging.Initialize("Client");

            Thread.Sleep(5000);

            var client = new NetClient("localhost", 45685);
            client.Start();

            var bytes = new byte[2 * 1024];
            var message = Encoding.UTF8.GetString(bytes);
            for (int i = 0; i < 3; i++)
            {
                client.SendMessage(message);
            }

            Thread.Sleep(1000);

            client.Stop();

            Thread.Sleep(1000);
        }
    }
}
