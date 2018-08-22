﻿using Lure.Net.Data;

namespace Lure.Net.Packets
{
    internal interface INetPacket
    {
        NetPacketDirection Direction { get; set; }

        void DeserializeHeader(INetDataReader reader);

        void DeserializeData(INetDataReader reader);

        void SerializeHeader(INetDataWriter writer);

        void SerializeData(INetDataWriter writer);
    }
}