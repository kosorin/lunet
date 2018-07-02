using System;
using System.Collections.Generic;
using Lure.Net.Data;
using Lure.Net.Packets;

namespace Lure.Net.Channels
{
    internal interface IMessageChannel : INetChannel
    {
        IEnumerable<RawMessage> GetReceivedRawMessages();

        void SendMessage(byte[] data);
    }
}