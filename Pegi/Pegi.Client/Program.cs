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

            for (int i = 0; i < 10; i++)
            {
                var message = new TestMessage
                {
                    Integer = i * 10,
                    Float = i * 1.5f,
                };
                client.SendMessage(message);
                Thread.Sleep(3000);
            }

            client.Stop();

            Thread.Sleep(1000);
        }
    }
}
