using System.Collections.Generic;

namespace Lunet.Channels
{
    public class ReliableMessageTracker
    {
        private readonly int _bufferSize;

        private readonly SeqNo?[] _packetSeqBuffer;
        private readonly List<ReliableMessage>[] _messageBuffer;

        public ReliableMessageTracker(int bufferSize)
        {
            _bufferSize = bufferSize;

            _packetSeqBuffer = new SeqNo?[_bufferSize];
            _messageBuffer = new List<ReliableMessage>[_bufferSize];
            for (var i = 0; i < _bufferSize; i++)
            {
                _messageBuffer[i] = new List<ReliableMessage>();
            }
        }

        /// <summary>
        /// Tracks messages.
        /// </summary>
        public void Track(SeqNo packetSeq, List<ReliableMessage> messageSeqs)
        {
            var index = GetIndex(packetSeq);

            _packetSeqBuffer[index] = packetSeq;

            var messages = _messageBuffer[index];
            messages.Clear();
            messages.AddRange(messageSeqs);
        }

        /// <summary>
        /// Gets tracked messages for packet seq.
        /// </summary>
        /// <returns>May return <c>null</c>.</returns>
        /// <remarks>Do not modify returned list.</remarks>
        public List<ReliableMessage>? Get(SeqNo packetSeq)
        {
            var index = GetIndex(packetSeq);

            return _packetSeqBuffer[index] == packetSeq
                ? _messageBuffer[index]
                : null;
        }

        /// <summary>
        /// Stops tracking messages for packet seq.
        /// </summary>
        public void Clear(SeqNo packetSeq)
        {
            var index = GetIndex(packetSeq);

            ref var packetSeqBufferValue = ref _packetSeqBuffer[index];
            if (packetSeqBufferValue.HasValue && packetSeqBufferValue.Value == packetSeq)
            {
                packetSeqBufferValue = null;
                _messageBuffer[index].Clear();
            }
        }

        private int GetIndex(SeqNo packetSeq)
        {
            return packetSeq.Value % _bufferSize;
        }
    }
}
