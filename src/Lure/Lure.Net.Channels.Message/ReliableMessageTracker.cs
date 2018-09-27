using Lure.Net.Packets;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Channels.Message
{
    internal class ReliableMessageTracker
    {
        private const int BufferSize = 1024;

        private readonly SeqNo[] _packetSeqBuffer = new SeqNo[BufferSize];
        private readonly List<SeqNo>[] _messageSeqBuffer = new List<SeqNo>[BufferSize];

        public ReliableMessageTracker()
        {
            for (int i = 0; i < BufferSize; i++)
            {
                _messageSeqBuffer[i] = new List<SeqNo>();
            }
        }

        /// <summary>
        /// Tracks sequenced messages.
        /// </summary>
        public void Track(SeqNo packetSeq, IEnumerable<SeqNo> messageSeqs)
        {
            var index = GetIndex(packetSeq);

            _packetSeqBuffer[index] = packetSeq;

            var messageSeqBuffer = _messageSeqBuffer[index];
            messageSeqBuffer.Clear();
            messageSeqBuffer.AddRange(messageSeqs);
        }

        /// <summary>
        /// Stops tracking sequenced messages and gets assigned message seqs to packet seq.
        /// May return <c>null</c>.
        /// </summary>
        public IEnumerable<SeqNo> Clear(SeqNo packetSeq)
        {
            var index = GetIndex(packetSeq);

            if (_packetSeqBuffer[index] == packetSeq)
            {
                return _messageSeqBuffer[index];
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
