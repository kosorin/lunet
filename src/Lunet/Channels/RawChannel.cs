using Lunet.Common;
using Lunet.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lunet.Channels
{
    public class RawChannel : Channel
    {
        private readonly Func<RawPacket> _packetActivator;

        private readonly List<byte[]> _outgoingDataQueue = new List<byte[]>();
        private readonly List<byte[]> _incomingDataQueue = new List<byte[]>();

        public RawChannel(byte id, Connection connection) : base(id, connection)
        {
            _packetActivator = ObjectActivatorFactory.Create<RawPacket>();
        }

        public override List<byte[]>? GetReceivedMessages()
        {
            lock (_incomingDataQueue)
            {
                if (_incomingDataQueue.Count == 0)
                {
                    return null;
                }

                var receivedMessages = _incomingDataQueue.ToList();
                _incomingDataQueue.Clear();
                return receivedMessages;
            }
        }

        public override void SendMessage(byte[] data)
        {
            lock (_outgoingDataQueue)
            {
                _outgoingDataQueue.Add(data);
            }
        }


        internal override void HandleIncomingPacket(NetDataReader reader)
        {
            var packet = _packetActivator();

            try
            {
                packet.DeserializeHeader(reader);
                packet.DeserializeData(reader);
            }
            catch (NetSerializationException)
            {
                return;
            }

            lock (_incomingDataQueue)
            {
                _incomingDataQueue.Add(packet.Data);
            }
        }

        internal override List<ChannelPacket>? CollectOutgoingPackets()
        {
            List<ChannelPacket>? outgoingPackets = null;

            lock (_outgoingDataQueue)
            {
                if (_outgoingDataQueue.Count > 0)
                {
                    // TODO: new
                    outgoingPackets = new List<ChannelPacket>(_outgoingDataQueue.Count);
                    foreach (var data in _outgoingDataQueue)
                    {
                        var packet = _packetActivator();
                        packet.Data = data;
                        outgoingPackets.Add(packet);
                    }
                    _outgoingDataQueue.Clear();
                }
            }

            return outgoingPackets;
        }
    }
}
