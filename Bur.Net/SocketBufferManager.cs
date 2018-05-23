using System.Collections.Generic;
using System.Net.Sockets;

namespace Bur.Net
{
    internal class SocketBufferManager
    {
        private readonly byte[] _buffer;
        private readonly int _bufferSize;
        private readonly int _capacity;
        private readonly Stack<int> _freeIndexPool;
        private int _currentIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="SocketBufferManager" /> class.
        /// </summary>
        /// <param name="count">Number of buffers.</param>
        /// <param name="size">Size of one buffer in bytes.</param>
        public SocketBufferManager(int count, int size)
        {
            _freeIndexPool = new Stack<int>();
            _capacity = size * count;
            _bufferSize = size;

            _currentIndex = 0;
            _buffer = new byte[_capacity];
        }

        public bool SetBuffer(SocketAsyncEventArgs args)
        {
            if (_freeIndexPool.Count > 0)
            {
                args.SetBuffer(_buffer, _freeIndexPool.Pop(), _bufferSize);
            }
            else
            {
                if ((_capacity - _bufferSize) < _currentIndex)
                {
                    return false;
                }
                args.SetBuffer(_buffer, _currentIndex, _bufferSize);
                _currentIndex += _bufferSize;
            }
            return true;
        }

        public void ClearBuffer(SocketAsyncEventArgs args)
        {
            _freeIndexPool.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }
    }
}
