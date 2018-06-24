using Lure.Net.Packets.Message;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Channels
{
    internal class SequencedRawMessageTracker
    {
        private const int BufferSize = 1024;

        private readonly SeqNo[] _packetSeqBuffer = new SeqNo[BufferSize];
        private readonly List<SeqNo>[] _rawMessageSeqBuffer = new List<SeqNo>[BufferSize];

        public SequencedRawMessageTracker()
        {
            for (int i = 0; i < BufferSize; i++)
            {
                _rawMessageSeqBuffer[i] = new List<SeqNo>();
            }
        }

        /// <summary>
        /// Assigns raw message seqs to packet seq.
        /// </summary>
        public void Track(SeqNo packetSeq, IEnumerable<ISequencedRawMessage> rawMessages)
        {
            var index = GetIndex(packetSeq);

            _packetSeqBuffer[index] = packetSeq;

            var rawMessageSeqs = _rawMessageSeqBuffer[index];
            rawMessageSeqs.Clear();
            rawMessageSeqs.AddRange(rawMessages.Select(x => x.Seq));
        }

        /// <summary>
        /// Gets assigned raw message seqs to packet seq.
        /// May return <c>null</c>.
        /// </summary>
        public IEnumerable<SeqNo> GetSeqs(SeqNo packetSeq)
        {
            var index = GetIndex(packetSeq);
            if (_packetSeqBuffer[index] == packetSeq)
            {
                return _rawMessageSeqBuffer[index];
            }
            else
            {
                return null;
            }
        }

        private int GetIndex(SeqNo packetSeq)
        {
            return packetSeq.Value % BufferSize;
        }
    }
}
