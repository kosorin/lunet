using Lure.Collections;
using Lure.Net.Data;
using Lure.Net.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Packets
{
    [PacketData(PacketDataType.PayloadReliable)]
    [PacketData(PacketDataType.PayloadReliableSequenced)]
    [PacketData(PacketDataType.PayloadReliableOrdered)]
    internal class ReliablePayloadPacketData : PayloadPacketData<ReliableRawMessage>
    {
    }
}
