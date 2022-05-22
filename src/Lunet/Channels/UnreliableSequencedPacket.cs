using Lunet.Data;

namespace Lunet.Channels;

public class UnreliableSequencedPacket : MessagePacket<UnreliableMessage>
{
    public UnreliableSequencedPacket(Func<UnreliableMessage> messageActivator) : base(messageActivator)
    {
    }

    public SeqNo Seq { get; set; }

    public override int HeaderLength => base.HeaderLength + SeqNo.SizeOf;

    protected override void DeserializeHeaderCore(NetDataReader reader)
    {
        Seq = reader.ReadSeqNo();
        base.DeserializeHeaderCore(reader);
    }

    protected override void SerializeHeaderCore(NetDataWriter writer)
    {
        writer.WriteSeqNo(Seq);
        base.SerializeHeaderCore(writer);
    }
}
