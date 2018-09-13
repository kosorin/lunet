﻿using Lure.Net.Data;
using Lure.Net.Packets;
using System;
using System.Collections.Generic;

namespace Lure.Net.Channels
{
    public interface INetChannel : IDisposable
    {
        void ProcessIncomingPacket(INetDataReader reader);

        IList<INetPacket> CollectOutgoingPackets();

        IList<byte[]> GetReceivedMessages();

        void SendMessage(byte[] data);
    }
}
