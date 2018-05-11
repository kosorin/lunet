using Bur.Common;
using Serilog;
using System;
using System.Net.Sockets;
using System.Threading;

namespace Bur.Net.Udp
{
    public class UdpConnectionListener : Runnable, IConnectionListener
    {
        private static readonly ILogger logger = Log.ForContext<UdpConnectionListener>();

        private readonly TcpListener listener;

        private readonly Thread thread;

        public UdpConnectionListener(UdpEndPoint endPoint)
        {
            EndPoint = endPoint;

            listener = new TcpListener(endPoint.Ip, endPoint.Port);
            thread = new Thread(Loop);
        }

        public UdpEndPoint EndPoint { get; }

        public event TypedEventHandler<IConnectionListener, ChannelConnectedEventArgs> ChannelConnected;

        public override void Start()
        {
            if (IsRunning)
            {
                return;
            }
            IsRunning = true;

            logger.Information("Start listening on {EndPoint}", EndPoint);
            listener.Start();
            thread.Start();
        }

        public override void Stop()
        {
            if (!IsRunning)
            {
                return;
            }
            IsRunning = false;

            logger.Information("Stopping listening...");
            listener.Stop();
            thread.Join();
            logger.Information("Stopped listening");
        }

        private void Loop()
        {
            try
            {
                LoopCore();
            }
            catch (Exception e)
            {
                logger.Fatal(e, "Unhandled listening exception");
            }
        }

        private void LoopCore()
        {
            while (IsRunning)
            {
                try
                {
                    var channel = AcceptChannel();
                    OnChannelConnected(channel);
                }
                catch (SocketException e) when (!IsRunning && e.SocketErrorCode == SocketError.Interrupted)
                {
                    logger.Debug("Listening interrupted");
                }
            }
        }

        private UdpChannel AcceptChannel()
        {
            var client = listener.AcceptTcpClient();
            var channel = new UdpChannel(client);
            logger.Verbose("Accepted new channel {RemoteEndPoint}", channel.RemoteEndPoint);
            return channel;
        }

        private void OnChannelConnected(UdpChannel channel)
        {
            ChannelConnected?.Invoke(this, new ChannelConnectedEventArgs(channel));
        }
    }
}
