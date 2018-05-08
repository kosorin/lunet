using Bur.Common;
using Serilog;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace Bur.Net.Tcp
{
    public class TcpChannel : Runnable, IChannel
    {
        private static readonly ILogger logger = Log.ForContext<TcpChannel>();

        private readonly byte[] buffer = new byte[4 * 1024];

        private readonly TcpClient client;

        public TcpChannel(TcpClient client)
        {
            this.client = client;

            var socket = client.Client;
            var ipEndPoint = (IPEndPoint)socket.RemoteEndPoint;
            RemoteEndPoint = new TcpEndPoint(ipEndPoint.Address, ipEndPoint.Port);
        }

        public IEndPoint RemoteEndPoint { get; }

        public event TypedEventHandler<IChannel, DataReceivedEventArgs> DataReceived;

        public event TypedEventHandler<IChannel> Stopped;

        public override void Start()
        {
            if (IsRunning)
            {
                return;
            }
            IsRunning = true;

            logger.Verbose("[{RemoteEndPoint}] Channel started", RemoteEndPoint);
            BeginRead();
        }

        public override void Stop()
        {
            if (!IsRunning)
            {
                return;
            }
            IsRunning = false;

            logger.Verbose("[{RemoteEndPoint}] Stopping channel", RemoteEndPoint);
            try
            {
                client.Close();
            }
            catch (Exception e)
            {
                logger.Error(e, "[{RemoteEndPoint}] Stop channel", RemoteEndPoint);
            }
            finally
            {
                OnStopped();
            }
        }

        public ConnectionState GetCurrentState()
        {
            try
            {
                var socket = client.Client;
                if (socket?.Connected == true)
                {
                    if (socket.Poll(0, SelectMode.SelectRead))
                    {
                        var buffer = new byte[1];
                        if (socket.Receive(buffer, SocketFlags.Peek) == 0)
                        {
                            return ConnectionState.Disconnected;
                        }
                        else
                        {
                            return ConnectionState.Connected;
                        }
                    }

                    return ConnectionState.Connected;
                }
                else
                {
                    return ConnectionState.Disconnected;
                }
            }
            catch
            {
                return ConnectionState.Disconnected;
            }
        }

        private void BeginRead()
        {
            var stream = client.GetStream();
            stream.BeginRead(buffer, 0, buffer.Length, ReadCallback, null);
        }

        private void ReadCallback(IAsyncResult ar)
        {
            if (!IsRunning)
            {
                return;
            }

            try
            {
                var stream = client.GetStream();
                var size = stream.EndRead(ar);
                logger.Verbose("[{RemoteEndPoint}] Received data (size={Size})", RemoteEndPoint, size);

                if (size > 0)
                {
                    var data = new byte[size];
                    Array.Copy(buffer, 0, data, 0, size);
                    OnDataReceived(data);

                    if (IsRunning)
                    {
                        BeginRead();
                    }
                }
                else
                {
                    logger.Verbose("[{RemoteEndPoint}] Channel closed by a remote host", RemoteEndPoint);
                    Stop();
                }
            }
            catch (IOException e) when (e.InnerException is SocketException se)
            {
                logger.Error(e, "[{RemoteEndPoint}] Unable to receive data ({SocketErrorCode}={ErrorCode})", RemoteEndPoint, se.SocketErrorCode, se.ErrorCode);
                Stop();
            }
            catch (Exception e)
            {
                logger.Error(e, "[{RemoteEndPoint}] Unable to receive data", RemoteEndPoint);
                Stop();
            }
        }

        private void OnDataReceived(byte[] data)
        {
            DataReceived?.Invoke(this, new DataReceivedEventArgs(data));
        }

        private void OnStopped()
        {
            Stopped?.Invoke(this, EventArgs.Empty);
        }
    }
}
