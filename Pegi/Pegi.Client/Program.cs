using Lure.Net;
using Lure.Net.Messages;
using System.Threading;

namespace Pegi.Client
{
    internal static class Program
    {
        private static void Main()
        {
            PegiLogging.Configure("Client");

            var client = new NetClient("localhost", 45685);
            client.Start();

            Thread.Sleep(2000);

            for (int i = 0; i < 10; i++)
            {
                var message = new TestMessage
                {
                    Integer = i * 10,
                    Float = i * 1.5f,
                };
                client.SendMessage(message);
                Thread.Sleep(500);
            }

            Thread.Sleep(2000);

            client.Stop();

            Thread.Sleep(1000);
        }
    }
}
