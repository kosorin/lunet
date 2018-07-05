using Lure.Net.Data;
using Lure.Net.Packets;
using System;
using System.Collections.Generic;

namespace Lure.Net.Channels
{
    internal interface INetChannel : IDisposable
    {
        byte Id { get; }

        void Update();

        void ReceivePacket(INetDataReader reader);

        IEnumerable<RawMessage> GetReceivedRawMessages();

        void SendMessage(byte[] data);
    }
}
