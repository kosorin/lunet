using Lure.Collections;
using Lure.Net.Data;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Packets
{
    [PacketData(PacketDataType.Payload)]
    internal class PayloadPacketData : PacketData, IPoolable
    {
        public override string DebuggerDisplay => $"Payload: {string.Join(", ", Messages.Select(x => x.Seq))}";

        public List<PayloadMessage> Messages { get; } = new List<PayloadMessage>();

        public override int Length => Messages.Sum(x => x.Length);

        public override void Deserialize(INetDataReader reader)
        {
            Messages.Clear();
            while (reader.Position < reader.Length)
            {
                var message = new PayloadMessage();
                message.Deserialize(reader);
                Messages.Add(message);
            }
        }

        public override void Serialize(INetDataWriter writer)
        {
            foreach (var message in Messages)
            {
                message.Serialize(writer);
            }
        }

        void IPoolable.OnRent()
        {
        }

        void IPoolable.OnReturn()
        {
            Messages.Clear();
        }
    }
}
