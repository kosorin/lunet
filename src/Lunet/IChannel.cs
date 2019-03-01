﻿using Lunet.Data;
using System.Collections.Generic;

namespace Lunet
{
    public interface IChannel
    {
        byte Id { get; }

        IConnection Connection { get; }


        void HandleIncomingPacket(NetDataReader reader);

        IList<IChannelPacket> CollectOutgoingPackets();

        IList<byte[]> GetReceivedMessages();

        void SendMessage(byte[] data);
    }
}