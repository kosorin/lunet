using Lunet.Data;

namespace Lunet.Channels
{
    public class ReliableMessage : Message
    {
        private long? _firstSendTimestamp;
        private long? _sendTimestamp;

        public long? FirstSendTimestamp => _firstSendTimestamp;

        public long? SendTimestamp
        {
            get => _sendTimestamp;
            set
            {
                _sendTimestamp = value;

                if (_firstSendTimestamp == null)
                {
                    _firstSendTimestamp = _sendTimestamp;
                }

                if (_sendTimestamp == null)
                {
                    _firstSendTimestamp = null;
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
}
