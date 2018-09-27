using Lure.Net.Data;
using Lure.Net.Packets;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Channels.Message
{
    public abstract class MessageChannel<TPacket, TMessage> : INetChannel
        where TPacket : MessagePacket<TMessage>
        where TMessage : Message
    {
        protected readonly Connection _connection;

        private readonly Func<TPacket> _packetActivator;
        private readonly Func<TMessage> _messageActivator;
        private readonly SimpleMessagePacker<TPacket, TMessage> _messagePacker;

        protected MessageChannel(Connection connection)
        {
            _connection = connection;

            _messageActivator = ObjectActivatorFactory.Create<TMessage>();
            _packetActivator = ObjectActivatorFactory.CreateWithValues<Func<TMessage>, TPacket>(_messageActivator);
            _messagePacker = new SimpleMessagePacker<TPacket, TMessage>(_packetActivator);

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
            foreach (var message in packet.Messages)
            {
                message.Timestamp = now;
                if (AcceptIncomingMessage(message))
                {
                    OnIncomingMessage(message);
                }
            }
        }

        public IList<INetPacket> CollectOutgoingPackets()
        {
            var outgoingMessages = GetOutgoingMessages();
            var outgoingPackets = PackOutgoingMessages(outgoingMessages);
            foreach (var packet in outgoingPackets)
            {
                OnOutgoingPacket(packet);

                var now = Timestamp.Current;
                foreach (var message in packet.Messages)
                {
                    message.Timestamp = now;
                }
            }
            return outgoingPackets.Cast<INetPacket>().ToList();
        }

        public abstract IList<byte[]> GetReceivedMessages();

        public void SendMessage(byte[] data)
        {
            var message = CreateOutgoingMessage(data);
            OnOutgoingMessage(message);
        }


        protected abstract bool AcceptIncomingPacket(TPacket packet);

        protected abstract bool AcceptIncomingMessage(TMessage message);

        protected abstract void OnIncomingPacket(TPacket packet);

        protected abstract void OnIncomingMessage(TMessage message);

        protected abstract bool AcknowledgeIncomingPacket(TPacket packet);


        protected virtual List<TPacket> PackOutgoingMessages(List<TMessage> messages)
        {
            // TODO: Řadit zprávy, aby se vhodně naplnil celý paket.
            // Např. k velké zprávě doplnit několik malých zpráv.
            // Pouze pro číslované zprávy.

            var packets = new List<TPacket>();

            if (messages.Count > 0)
            {
                var packet = CreateOutgoingPacket();
                var packetLength = 0; // TODO: Include packet length (without messages)
                foreach (var message in messages)
                {
                    if (packetLength + message.Length > _connection.MTU)
                    {
                        packets.Add(packet);

                        packet = CreateOutgoingPacket();
                        packetLength = 0;
                    }
                    packet.Messages.Add(message);
                    packetLength += message.Length;
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

        protected abstract List<TMessage> GetOutgoingMessages();

        protected TMessage CreateOutgoingMessage(byte[] data)
        {
            var message = _messageActivator();
            message.Timestamp = null;
            message.Data = data;

            PrepareOutgoingMessage(message);

            return message;
        }

        protected abstract void PrepareOutgoingPacket(TPacket packet);

        protected abstract void PrepareOutgoingMessage(TMessage message);

        protected abstract void OnOutgoingPacket(TPacket packet);

        protected abstract void OnOutgoingMessage(TMessage message);
    }
}
