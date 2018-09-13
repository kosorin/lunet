using Lure.Net.Data;
using System;

namespace Lure.Net.Packets
{
    public interface INetPacket
    {
        void DeserializeHeader(INetDataReader reader);

        void DeserializeData(INetDataReader reader);

        void SerializeHeader(INetDataWriter writer);

        void SerializeData(INetDataWriter writer);
    }
}