using Lure.Net.Packets;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Channels
{
    internal class ReliableRawMessageTracker
    {
        private const int BufferSize = 1024;

        private readonly SeqNo[] _packetSeqBuffer = new SeqNo[BufferSize];
        private readonly List<SeqNo>[] _rawMessageSeqBuffer = new List<SeqNo>[BufferSize];

        public ReliableRawMessageTracker()
        {
            for (int i = 0; i < BufferSize; i++)
            {
                _rawMessageSeqBuffer[i] = new List<SeqNo>();
            }
        }

        /// <summary>
        /// Tracks sequenced raw messages.
        /// </summary>
        public void Track(SeqNo packetSeq, IEnumerable<SeqNo> rawMessageSeqs)
        {
            var index = GetIndex(packetSeq);

            _packetSeqBuffer[index] = packetSeq;

            var rawMessageSeqBuffer = _rawMessageSeqBuffer[index];
            rawMessageSeqBuffer.Clear();
            rawMessageSeqBuffer.AddRange(rawMessageSeqBuffer);
        }

        /// <summary>
        /// Stops tracking sequenced raw messages and gets assigned raw message seqs to packet seq.
        /// May return <c>null</c>.
        /// </summary>
        public IEnumerable<SeqNo> Clear(SeqNo packetSeq)
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
