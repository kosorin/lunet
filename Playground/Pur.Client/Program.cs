using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Pur.Client
{
    internal static class Program
    {
        private static int Main()
        {
            Console.WriteLine("Client");

            var hostName = "localhost";
            var port = 45698;

            Thread.Sleep(1000);

            var encoding = Encoding.UTF8;
            try
            {
                var client = new TcpClient();

                Console.WriteLine("Connecting...");
                client.Connect(hostName, port);
                Console.WriteLine("Connected");

                var ns = client.GetStream();

                using (var br = new BinaryReader(ns, encoding, true))
                using (var bw = new BinaryWriter(ns, encoding, true))
                {
                    bw.Write("Zdar");
                }

                Thread.Sleep(5000);

                ns.Close();

                client.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return 0;
        }
    }
}
