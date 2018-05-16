using Bur.Common;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Bur.Net.Udp
{
    public class NetConnection
    {
        public const int StopTimeout = 1; // 1 second

        public const int ReceiveBufferSize = 64 * 1024; // 64 kB


        private static readonly ILogger logger = Log.ForContext<NetConnection>();

        private byte[] receiveBuffer = new byte[ReceiveBufferSize];

        private readonly Socket socket;

        private NetConnection(Socket socket)
        {
            this.socket = socket;
        }




        public static NetConnection CreateClient(AddressFamily family, string hostName, int port)
        {
            if (!AddressHelpers.ValidateAddressFamily(family))
            {
                throw new ArgumentException($"{nameof(NetConnection)} only accept {AddressFamily.InterNetwork} or {AddressFamily.InterNetworkV6} addresses.", nameof(family));
            }

            if (hostName == null)
            {
                throw new ArgumentNullException(nameof(hostName));
            }

            if (!EndPointHelpers.ValidatePortNumber(port))
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            Socket socket = null;

            var addresses = Dns.GetHostAddresses(hostName);
            foreach (var address in addresses.Where(x => x.AddressFamily == family))
            {
                try
                {
                    socket = new Socket(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                    socket.Connect(address, port);
                    logger.Debug("Connect from {LocalEndPoint} to {RemoteEndPoint}", socket.LocalEndPoint, socket.RemoteEndPoint);
                    break;
                }
                catch (Exception e)
                {
                    if (socket != null)
                    {
                        socket.Close();
                        socket = null;
                    }
                }
            }

            if (socket == null)
            {
                throw new SocketException((int)SocketError.NotConnected);
            }

            return new NetConnection(socket);
        }





        public static NetConnection CreateServer(AddressFamily family, int port)
        {
            if (!AddressHelpers.ValidateAddressFamily(family))
            {
                throw new ArgumentException($"{nameof(NetConnection)} only accept {AddressFamily.InterNetwork} or {AddressFamily.InterNetworkV6} addresses.", nameof(family));
            }

            if (!EndPointHelpers.ValidatePortNumber(port))
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            var address = AddressHelpers.GetAny(family);
            var localEndPoint = new IPEndPoint(address, port);
            var socket = new Socket(family, SocketType.Dgram, ProtocolType.Udp);

            if (family == AddressFamily.InterNetworkV6)
            {
                socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            }
            socket.Bind(localEndPoint);

            return new NetConnection(socket);
        }








        public void Start()
        {
            //if (IsRunning)
            //{
            //    return;
            //}
            //IsRunning = true;

            logger.Verbose("Peer started");
            BeginReceive();
        }

        public void Stop()
        {
            //if (!IsRunning)
            //{
            //    return;
            //}
            //IsRunning = false;

            logger.Verbose("Stopping peer");
            try
            {
                socket.Close(StopTimeout);
            }
            catch (Exception e)
            {
                logger.Error(e, "Stop peer");
            }
        }

        public void EnqueueMessage(string message)
        {
            var sendBuffer = Encoding.UTF8.GetBytes(message);
            socket.BeginSend(sendBuffer, 0, sendBuffer.Length, SocketFlags.None, SendCallback, null);
        }

        public void EnqueueMessageTo(IPEndPoint remoteEndPoint, string message)
        {
            var sendBuffer = Encoding.UTF8.GetBytes(message);
            socket.BeginSendTo(sendBuffer, 0, sendBuffer.Length, SocketFlags.None, remoteEndPoint, SendToCallback, remoteEndPoint);
        }


        private void BeginReceive()
        {
            //if (!IsRunning)
            //{
            //    return;
            //}

            var remoteEndPoint = (EndPoint)EndPointHelpers.GetAny(socket.AddressFamily);
            socket.BeginReceiveFrom(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ref remoteEndPoint, ReceiveCallback, null);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            //if (!IsRunning)
            //{
            //    return;
            //}

            var remoteEndPoint = (EndPoint)EndPointHelpers.GetAny(socket.AddressFamily);
            try
            {
                var size = socket.EndReceiveFrom(ar, ref remoteEndPoint);
                logger.Verbose("[{RemoteEndPoint}] Received data (size={Size})", remoteEndPoint, size);

                if (size > 0)
                {
                    var data = new byte[size];
                    Array.Copy(receiveBuffer, 0, data, 0, size);
                }
                else
                {
                    logger.Verbose("[{RemoteEndPoint}] Channel closed by a remote host", remoteEndPoint);
                    Stop();
                }
            }
            catch (SocketException e)
            {
                var isError = true;
                switch (e.SocketErrorCode)
                {
                case SocketError.MessageSize:
                    logger.Warning("[{RemoteEndPoint}] Received data are too big (size>{ReceiveBufferSize})", remoteEndPoint, ReceiveBufferSize);
                    isError = false;
                    break;
                }

                if (isError)
                {
                    logger.Error(e, "[{RemoteEndPoint}] Unable to receive data ({SocketErrorCode}:{ErrorCode})", remoteEndPoint, e.SocketErrorCode, e.ErrorCode);
                    Stop();
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "[{RemoteEndPoint}] Unable to receive data", remoteEndPoint);
                Stop();
            }

            BeginReceive();
        }


        private void SendCallback(IAsyncResult ar)
        {
            //if (!IsRunning)
            //{
            //    return;
            //}

            try
            {
                var size = socket.EndSend(ar);
                logger.Verbose("[{RemoteEndPoint}] Sent data (size={Size})", socket.RemoteEndPoint, size);
            }
            catch (SocketException e)
            {
                logger.Error(e, "[{RemoteEndPoint}] Unable to send data ({SocketErrorCode}={ErrorCode})", socket.RemoteEndPoint, e.SocketErrorCode, e.ErrorCode);
                Stop();
            }
            catch (Exception e)
            {
                logger.Error(e, "[{RemoteEndPoint}] Unable to send data", socket.RemoteEndPoint);
                Stop();
            }
        }

        private void SendToCallback(IAsyncResult ar)
        {
            //if (!IsRunning)
            //{
            //    return;
            //}

            var remoteEndPoint = (IPEndPoint)ar.AsyncState;
            try
            {
                var size = socket.EndSendTo(ar);
                logger.Verbose("[{RemoteEndPoint}] Sent data (size={Size})", remoteEndPoint, size);
            }
            catch (SocketException e)
            {
                logger.Error(e, "[{RemoteEndPoint}] Unable to send data ({SocketErrorCode}={ErrorCode})", remoteEndPoint, e.SocketErrorCode, e.ErrorCode);
                Stop();
            }
            catch (Exception e)
            {
                logger.Error(e, "[{RemoteEndPoint}] Unable to send data", remoteEndPoint);
                Stop();
            }
        }
    }
}
