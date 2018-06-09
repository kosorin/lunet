using Lure.Net.Messages;

namespace Lure.Net.Packets
{
    internal class PayloadMessage
    {
        public PayloadMessage(ushort id, byte[] data)
        {
            Id = id;
            Data = data;
        }

        public ushort Id { get; }

        public byte[] Data { get; }

        public long? LastSendTimestamp { get; set; }
    }
}
