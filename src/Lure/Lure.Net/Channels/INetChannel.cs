using Lure.Net.Data;
using System;
using System.Collections.Generic;

namespace Lure.Net.Channels
{
    internal interface INetChannel : IDisposable
    {
        byte Id { get; }

        void Update();

        void ProcessIncomingPacket(INetDataReader reader);

        IList<byte[]> GetReceivedMessages();

        void SendMessage(byte[] data);
    }
}
