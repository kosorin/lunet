using Lure.Net.Data;
using System;

namespace Lure.Net.Tcp
{
    internal class TcpStreamParser
    {
        private enum State
        {
            Header,
            Data,
        }

        private readonly NetDataReader _headerReader;
        private readonly NetDataWriter _headerWriter;

        private int _dataLength;
        private readonly NetDataWriter _dataBuffer;

        private State _state;

        public TcpStreamParser()
        {
            var headerBuffer = new byte[sizeof(ushort)];
            _headerReader = new NetDataReader(headerBuffer, 0, headerBuffer.Length);
            _headerWriter = new NetDataWriter(headerBuffer, 0, headerBuffer.Length);

            _dataLength = 0;
            _dataBuffer = new NetDataWriter();

            _state = State.Header;
        }

        /// <summary>
        /// Gets data of last parsed packet.
        /// </summary>
        public INetBuffer Buffer => _dataBuffer;

        /// <summary>
        /// Reads data until it parse entire packet.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns>Returns <c>true</c> if new packet is available.</returns>
        public bool Next(NetDataReader reader)
        {
            if (_state == State.Header)
            {
                ParseHeader(reader);
            }

            if (_state == State.Data)
            {
                return ParseData(reader);
            }

            return false;
        }

        private void ParseHeader(NetDataReader reader)
        {
            var headerLeft = _headerWriter.Capacity - _headerWriter.Length;
            var headerToRead = Math.Min(headerLeft, reader.Length - reader.Position);
            for (int i = 0; i < headerToRead; i++)
            {
                _headerWriter.WriteByte(reader.ReadByte());
            }
            headerLeft -= headerToRead;

            if (headerLeft == 0)
            {
                _headerReader.Seek();
                _dataLength = _headerReader.ReadUShort();

                if (_dataLength < 0)
                {
                    throw new NetException("Bad TCP data.");
                }
                else
                {
                    _state = State.Data;
                    _dataBuffer.Reset();
                }
            }
        }

        private bool ParseData(NetDataReader reader)
        {
            var dataLeft = _dataLength - _dataBuffer.Length;
            var dataToRead = Math.Min(dataLeft, reader.Length - reader.Position);
            if (dataToRead > 0)
            {
                _dataBuffer.WriteBytes(reader.ReadBytes(dataToRead));
                dataLeft -= dataToRead;
            }

            if (dataLeft == 0)
            {
                _state = State.Header;
                _headerWriter.Reset();
                return true;
            }

            return false;
        }
    }
}
