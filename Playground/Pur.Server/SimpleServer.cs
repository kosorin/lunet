using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Pur.Server
{
    public class SimpleServer
    {
        private readonly Thread loopThread;

        private readonly TcpListener listener;

        private bool stopLoop;

        public SimpleServer(string hostName, int port)
        {
            IP = IPAddress.Parse(hostName);
            Port = port;

            loopThread = new Thread(Loop);
            listener = new TcpListener(IP, Port);
        }

        public event EventHandler<ConnectedEventArgs> Connected;


        public Encoding Encoding { get; } = Encoding.UTF8;

        public IPAddress IP { get; }

        public int Port { get; }


        public void Start()
        {
            Console.WriteLine($"[LOOP {Thread.CurrentThread.ManagedThreadId}] start");
            stopLoop = false;
            listener.Start();
            loopThread.Start();
        }

        public void Stop()
        {
            Console.WriteLine($"[LOOP {Thread.CurrentThread.ManagedThreadId}] stop");
            stopLoop = true;
            listener.Stop();
            loopThread.Join();
            Console.WriteLine($"[LOOP {Thread.CurrentThread.ManagedThreadId}] joined");
        }

        private void Loop()
        {
            try
            {
                LoopCore();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[LOOP {Thread.CurrentThread.ManagedThreadId}] UNHANDLED ERROR: {e.Message}");
            }
        }

        private void LoopCore()
        {
            var resetEvent = new ManualResetEvent(false);

            while (!stopLoop)
            {
                try
                {
                    //resetEvent.Reset();
                    //listener.BeginAcceptTcpClient(AcceptTcpClientCallback, resetEvent);
                    //resetEvent.WaitOne(1000);

                    try
                    {
                        var client = listener.AcceptTcpClient();
                        OnConnected(client);
                    }
                    catch (SocketException e) when (e.SocketErrorCode == SocketError.Interrupted)
                    {
                        Console.WriteLine("cancelled");
                        return;
                    }

                    Console.WriteLine($"[LOOP {Thread.CurrentThread.ManagedThreadId}] Client connected completed");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[LOOP {Thread.CurrentThread.ManagedThreadId}] ERROR: {e.Message}");
                    return;
                }
            }
        }

        private void AcceptTcpClientCallback(IAsyncResult ar)
        {
            var resetEvent = (ManualResetEvent)ar.AsyncState;
            var client = listener.EndAcceptTcpClient(ar);

            OnConnected(client);

            resetEvent.Set();
        }

        private void OnConnected(TcpClient client)
        {
            Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] Client connected");
            Connected?.Invoke(this, new ConnectedEventArgs(client));
        }
    }
}
