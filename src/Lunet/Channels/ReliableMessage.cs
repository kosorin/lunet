using Lunet.Data;

namespace Lunet.Channels;

public class ReliableMessage : Message
{
    private long? _sendTimestamp;

    public long? FirstSendTimestamp { get; private set; }

    public long? SendTimestamp
    {
        get => _sendTimestamp;
        set
        {
            _sendTimestamp = value;

            if (FirstSendTimestamp == null)
            {
                FirstSendTimestamp = _sendTimestamp;
            }

            if (_sendTimestamp == null)
            {
                FirstSendTimestamp = null;
            }
        }
    }

    public SeqNo Seq { get; set; }

    public override int HeaderLength => SeqNo.SizeOf + base.HeaderLength;

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
