using Lure.Net.Data;
using System;

namespace Lure.Net.Packets
{
    public interface INetPacket
    {
        void DeserializeHeader(NetDataReader reader);

        void DeserializeData(NetDataReader reader);

        void SerializeHeader(NetDataWriter writer);

        void SerializeData(NetDataWriter writer);
    }
}