using Lunet.Data;

namespace Lunet.Channels
{
    public class ReliableMessage : Message
    {
        private long? _firstActionTimestamp;
        private long? _timestamp;

        public long? FirstActionTimestamp => _firstActionTimestamp;

        public long? Timestamp
        {
            get => _timestamp;
            set
            {
                _timestamp = value;

                if (_firstActionTimestamp == null)
                {
                    _firstActionTimestamp = _timestamp;
                }

                if (_timestamp == null)
                {
                    _firstActionTimestamp = null;
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
