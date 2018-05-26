using Lure.Net;
using System.Text;
using System.Threading;

namespace Pegi.Client
{
    internal static class Program
    {
        private static void Main()
        {
            PegiLogging.Configure("Client");

            Thread.Sleep(2000);

            var client = new NetClient("localhost", 45685);
            client.Start();

            for (int i = 0; i < 3; i++)
            {
                var bytes = new byte[(i + 1) * 128];
                var message = new TextMessage
                {
                    Text = Encoding.UTF8.GetString(bytes)
                };

                for (int k = 0; k < 5; k++)
                {
                    client.SendMessage(message);
                }
                Thread.Sleep(1000);
            }

            client.Stop();

            Thread.Sleep(1000);
        }
    }
}
