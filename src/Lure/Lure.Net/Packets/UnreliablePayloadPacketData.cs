using Lure.Collections;
using Lure.Net.Data;
using Lure.Net.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Packets
{
    [PacketData(PacketDataType.PayloadUnreliable)]
    [PacketData(PacketDataType.PayloadUnreliableSequenced)]
    internal class UnreliablePayloadPacketData : PayloadPacketData<RawMessage>
    {
    }
}
