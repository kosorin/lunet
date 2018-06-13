using Lure.Net.Messages;

namespace Lure.Net.Packets
{
    internal class PayloadMessage : IPacketPart
    {
        public PayloadMessage(SeqNo seq, byte[] data)
        {
            Seq = seq;
            Data = data;
        }

        public SeqNo Seq { get; }

        public byte[] Data { get; }

        public long? LastSendTimestamp { get; set; }

        public int Length => Seq.Length + Data.Length;
    }
}
