using Lunet.Data;
using Lunet.Extensions;
using System;

namespace Lunet.Channels
{
    public class ReliableMessage : Message
    {
        public long? Timestamp { get; set; }

        public SeqNo Seq { get; set; }

        public override int HeaderLength => SeqNo.SizeOf;

        public override void Deserialize(NetDataReader reader)
        {
            Seq = reader.ReadSeqNo();
            base.Deserialize(reader);
        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.WriteSeqNo(Seq);
            base.Serialize(writer);
        }
    }
}
