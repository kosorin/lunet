using Lure.Net.Data;
using System;

namespace Lure.Net.Channels
{
    internal interface INetChannel : IDisposable
    {
        byte Id { get; }

        void Update();

        void ReceivePacket(NetDataReader reader);
    }
}