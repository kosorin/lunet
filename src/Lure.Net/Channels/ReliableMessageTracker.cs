using System.Collections.Generic;

namespace Lure.Net.Channels
{
    public class ReliableMessageTracker
    {
        private const int BufferSize = 1024;

        private readonly SeqNo?[] _packetSeqBuffer = new SeqNo?[BufferSize];
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

            var messageSeqs = _messageSeqBuffer[index];
            if (_packetSeqBuffer[index] == packetSeq)
            {
                messageSeqs = _messageSeqBuffer[index];
            }
            else
            {
                messageSeqs = null;
            }
            _packetSeqBuffer[index] = null;
            return messageSeqs;
        }

        private static int GetIndex(SeqNo packetSeq)
        {
            return packetSeq.Value % BufferSize;
        }
    }
}
