using System.Collections.Generic;
using System.Net.Sockets;

namespace Lure.Net
{
    internal class TokenBufferManager
    {
        private readonly byte[] _buffer;
        private readonly int _bufferSize;
        private readonly int _capacity;
        private readonly Queue<int> _freeIndexPool;
        private int _currentIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenBufferManager" /> class.
        /// </summary>
        /// <param name="count">Number of buffers.</param>
        /// <param name="bufferSize">Size of one buffer in bytes.</param>
        public TokenBufferManager(int count, int bufferSize)
        {
            _freeIndexPool = new Queue<int>();
            _capacity = bufferSize * count;
            _bufferSize = bufferSize;

            _currentIndex = 0;
            _buffer = new byte[_capacity];
        }


        public void SetBuffer(SocketAsyncEventArgs token)
        {
            if (_freeIndexPool.TryDequeue(out var freeIndex))
            {
                token.SetBuffer(_buffer, freeIndex, _bufferSize);
            }
            else
            {
                if (_currentIndex + _bufferSize >= _capacity)
                {
                    throw new NetException("Set socket buffer.");
                }
                token.SetBuffer(_buffer, _currentIndex, _bufferSize);
                _currentIndex += _bufferSize;
            }
        }

        public void ClearBuffer(SocketAsyncEventArgs token)
        {
            _freeIndexPool.Enqueue(token.Offset);
            token.SetBuffer(null, 0, 0);
        }
    }
}
