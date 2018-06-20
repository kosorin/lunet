using Lure.Net.Data;
using System;

namespace Lure.Net.Packets
{
    internal interface IPacketPart
    {
        int Length { get; }

        void Deserialize(INetDataReader reader);

        void Serialize(INetDataWriter writer);
    }
}
