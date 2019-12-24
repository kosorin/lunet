﻿using Lunet.Common;
using Lunet.Data;
using Lunet.Extensions;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Lunet
{
    internal class UdpPacket : PoolableObject<UdpPacket>
    {
        public static Guid Version { get; } = Guid.Parse("1EDEFE8C-9469-4D68-9F3E-40A4A1971B90");

        public static uint VersionHash { get; } = Crc32.Compute(Version.ToByteArray());


        private readonly IPEndPoint _receiveRemoteEndPoint;
        private readonly byte[] _buffer;

        public UdpPacket(AddressFamily addressFamily)
        {
            _receiveRemoteEndPoint = addressFamily.GetAnyEndPoint();
            _buffer = new byte[ushort.MaxValue];

            Reader = new NetDataReader(_buffer);
            Writer = new NetDataWriter(_buffer);

            Operation = new SocketAsyncEventArgs
            {
                UserToken = this,
            };
        }


        public InternetEndPoint RemoteEndPoint { get; set; }

        public NetDataReader Reader { get; }

        public NetDataWriter Writer { get; }

        public SocketAsyncEventArgs Operation { get; }


        public void BeginReceive()
        {
            Operation.RemoteEndPoint = _receiveRemoteEndPoint;
            Operation.SetBuffer(Reader.GetDataMemory());
        }

        public bool EndReceive()
        {
            if (IsOperationSuccessful())
            {
                Reader.Reset(Operation.BytesTransferred);

                if (!Crc32.Check(VersionHash, Reader.GetReadOnlySpan()))
                {
                    return false;
                }

                Reader.Reset(Operation.BytesTransferred - Crc32.HashLength);

                RemoteEndPoint = new InternetEndPoint(Operation.RemoteEndPoint);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void BeginSend()
        {
            Writer.Flush();
            var hash = Crc32.Append(VersionHash, Writer.GetReadOnlySpan());
            Writer.WriteHash(hash);
            Writer.Flush();

            Operation.RemoteEndPoint = RemoteEndPoint.EndPoint;
            Operation.SetBuffer(Writer.GetMemory());
        }

        public bool EndSend()
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
