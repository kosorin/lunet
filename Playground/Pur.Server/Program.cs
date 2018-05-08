using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pur.Server
{
    class Server
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine($"[LOOP {Thread.CurrentThread.ManagedThreadId}] Server");

            var hostName = "127.0.0.1";
            var port = 45698;


            var server = new SimpleServer(hostName, port);
            var clients = new ConcurrentBag<TcpClient>();

            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                Console.WriteLine($"[LOOP {Thread.CurrentThread.ManagedThreadId}] CANCEL");
                server.Stop();
            };

            server.Start();
            server.Connected += Server_Connected;


            Console.WriteLine($"[LOOP {Thread.CurrentThread.ManagedThreadId}] run");
            await Task.Delay(12 * 1000);
            Console.WriteLine($"[LOOP {Thread.CurrentThread.ManagedThreadId}] end");
        }

        private static void Server_Connected(object sender, ConnectedEventArgs e)
        {
            using (var bw = new BinaryWriter(e.Client.GetStream(), Encoding.UTF8, true))
            {
                bw.Write("Hello");
            }
        }
    }
}
