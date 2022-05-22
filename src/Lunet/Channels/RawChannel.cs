using Lunet.Common;
using Lunet.Data;

namespace Lunet.Channels;

public class RawChannel : Channel<RawPacket>
{
    private readonly List<byte[]> _outgoingDataQueue = new List<byte[]>();
    private readonly List<byte[]> _incomingDataQueue = new List<byte[]>();

    public RawChannel(byte id, Connection connection) : base(id, connection)
    {
        PacketActivator = ObjectActivatorFactory.Create<RawPacket>();
    }

    protected override Func<RawPacket> PacketActivator { get; }


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
        var packet = PacketActivator();

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
                    var packet = PacketActivator();
                    packet.Data = data;
                    outgoingPackets.Add(packet);
                }
                _outgoingDataQueue.Clear();
            }
        }

        return outgoingPackets;
    }
}
