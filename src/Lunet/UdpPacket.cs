using Lunet.Common;
using Lunet.Data;
using Lunet.Extensions;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Lunet
{
    internal class UdpPacket : PoolableObject<UdpPacket>
    {
        private readonly IPEndPoint _receiveRemoteEndPoint;
        private readonly byte[] _buffer;

        public UdpPacket(AddressFamily addressFamily)
        {
            _receiveRemoteEndPoint = addressFamily.GetAnyEndPoint();
            _buffer = new byte[ushort.MaxValue];

            RemoteEndPoint = new UdpEndPoint(_receiveRemoteEndPoint);

            Reader = new NetDataReader(_buffer);
            Writer = new NetDataWriter(_buffer);

            Operation = new SocketAsyncEventArgs
            {
                UserToken = this,
            };
        }


        public UdpEndPoint RemoteEndPoint { get; set; }

        public NetDataReader Reader { get; }

        public NetDataWriter Writer { get; }

        public SocketAsyncEventArgs Operation { get; }


        internal void BeginReceive()
        {
            Operation.RemoteEndPoint = _receiveRemoteEndPoint;
            Operation.SetBuffer(Reader.GetDataMemory());
        }

        internal bool EndReceive()
        {
            if (IsOperationSuccessful())
            {
                Reader.Reset(Operation.BytesTransferred);

                // TODO: new
                RemoteEndPoint = new UdpEndPoint(Operation.RemoteEndPoint);
                return true;
            }
            else
            {
                return false;
            }
        }

        internal void BeginSend()
        {
            Writer.Flush();

            Operation.RemoteEndPoint = RemoteEndPoint.EndPoint;
            Operation.SetBuffer(Writer.GetMemory());
        }

        internal bool EndSend()
        {
            return IsOperationSuccessful();
        }


        private bool IsOperationSuccessful()
        {
            return Operation.SocketError == SocketError.Success && Operation.BytesTransferred > 0;
        }


        protected override void OnRent()
        {
            Reader.Reset();
            Writer.Reset();
        }

        protected override void OnReturn()
        {
        }


        private int _disposed;

        protected override void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 1)
            {
                return;
            }

            if (disposing)
            {
                Operation.UserToken = null;
                Operation.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
