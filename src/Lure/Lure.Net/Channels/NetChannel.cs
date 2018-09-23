using Lure.Net.Data;
using Lure.Net.Packets;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Channels
{
    public abstract class NetChannel<TPacket, TRawMessage> : INetChannel
        where TPacket : NetPacket<TRawMessage>
        where TRawMessage : RawMessage
    {
        protected readonly Connection _connection;

        private readonly Func<TPacket> _packetActivator;
        private readonly Func<TRawMessage> _rawMessageActivator;

        protected NetChannel(Connection connection)
        {
            _connection = connection;

            _rawMessageActivator = ObjectActivatorFactory.Create<TRawMessage>();
            _packetActivator = ObjectActivatorFactory.CreateWithValues<Func<TRawMessage>, TPacket>(_rawMessageActivator);

            //Logger = Log.ForContext(GetType());
            Logger = new LoggerConfiguration().CreateLogger();
        }


        protected ILogger Logger { get; }


        public void ProcessIncomingPacket(NetDataReader reader)
        {
            var packet = _packetActivator();

            try
            {
                packet.DeserializeHeader(reader);
            }
            catch (NetSerializationException)
            {
                Logger.Warning("Bad packet header");
                return;
            }

            if (!AcceptIncomingPacket(packet))
            {
                return;
            }

            try
            {
                packet.DeserializeData(reader);
            }
            catch (NetSerializationException)
            {
                Logger.Warning("Bad packet data");
                return;
            }

            if (!AcknowledgeIncomingPacket(packet))
            {
                return;
            }

            OnIncomingPacket(packet);

            var now = Timestamp.Current;
            foreach (var rawMessage in packet.RawMessages)
            {
                rawMessage.Timestamp = now;
                if (AcceptIncomingRawMessage(rawMessage))
                {
                    OnIncomingRawMessage(rawMessage);
                }
            }
        }

        public IList<INetPacket> CollectOutgoingPackets()
        {
            var outgoingRawMessages = GetOutgoingRawMessages();
            var outgoingPackets = PackOutgoingRawMessages(outgoingRawMessages);
            foreach (var packet in outgoingPackets)
            {
                OnOutgoingPacket(packet);

                var now = Timestamp.Current;
                foreach (var rawMessage in packet.RawMessages)
                {
                    rawMessage.Timestamp = now;
                }
            }
            return outgoingPackets.Cast<INetPacket>().ToList();
        }

        public abstract IList<byte[]> GetReceivedMessages();

        public void SendMessage(byte[] data)
        {
            var rawMessage = CreateOutgoingRawMessage(data);
            OnOutgoingRawMessage(rawMessage);
        }


        protected abstract bool AcceptIncomingPacket(TPacket packet);

        protected abstract bool AcceptIncomingRawMessage(TRawMessage rawMessage);

        protected abstract void OnIncomingPacket(TPacket packet);

        protected abstract void OnIncomingRawMessage(TRawMessage rawMessage);

        protected abstract bool AcknowledgeIncomingPacket(TPacket packet);


        protected virtual List<TPacket> PackOutgoingRawMessages(List<TRawMessage> rawMessages)
        {
            // TODO: Řadit zprávy, aby se vhodně naplnil celý paket.
            // Např. k velké zprávě doplnit několik malých zpráv.
            // Pouze pro číslované zprávy.

            var packets = new List<TPacket>();

            if (rawMessages.Count > 0)
            {
                var packet = CreateOutgoingPacket();
                var packetLength = 0; // TODO: Include packet length (without messages)
                foreach (var rawMessage in rawMessages)
                {
                    if (packetLength + rawMessage.Length > _connection.MTU)
                    {
                        packets.Add(packet);

                        packet = CreateOutgoingPacket();
                        packetLength = 0;
                    }
                    packet.RawMessages.Add(rawMessage);
                    packetLength += rawMessage.Length;
                }
                if (packetLength > 0)
                {
                    packets.Add(packet);
                }
            }

            return packets;
        }

        protected TPacket CreateOutgoingPacket()
        {
            var packet = _packetActivator();

            PrepareOutgoingPacket(packet);

            return packet;
        }

        protected abstract List<TRawMessage> GetOutgoingRawMessages();

        protected TRawMessage CreateOutgoingRawMessage(byte[] data)
        {
            var rawMessage = _rawMessageActivator();
            rawMessage.Timestamp = null;
            rawMessage.Data = data;

            PrepareOutgoingRawMessage(rawMessage);

            return rawMessage;
        }

        protected abstract void PrepareOutgoingPacket(TPacket packet);

        protected abstract void PrepareOutgoingRawMessage(TRawMessage rawMessage);

        protected abstract void OnOutgoingPacket(TPacket packet);

        protected abstract void OnOutgoingRawMessage(TRawMessage rawMessage);
    }
}
