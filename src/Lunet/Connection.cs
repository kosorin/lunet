using Lunet.Common;
using Lunet.Data;
using Lunet.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Lunet
{
    public abstract class Connection : IDisposable
    {
        public static Guid Version { get; } = Guid.Parse("1EDEFE8C-9469-4D68-9F3E-40A4A1971B90");

        public static uint VersionHash { get; } = Crc32.Compute(Version.ToByteArray());

        private static ILog Log { get; } = LogProvider.GetCurrentClassLogger();


        private volatile ConnectionState _state;

        private readonly ChannelCollection _channels;

        private readonly object _pingLock = new object();
        private readonly int _pingInterval = 1_000;
        private SeqNo _pingSequence;
        private long _pingTimestamp;
        private int _rtt;

        private readonly object _fragmentLock = new object();
        private readonly int _fragmentTimeout = 10_000;
        private readonly ObjectPool<FragmentGroup> _fragmentGroupPool = new ObjectPool<FragmentGroup>();
        private readonly ObjectPool<Fragment> _fragmentPool = new ObjectPool<Fragment>();
        private readonly Dictionary<SeqNo, FragmentGroup> _fragmentGroups = new Dictionary<SeqNo, FragmentGroup>();
        private SeqNo _fragmentSeq = SeqNo.Zero;

        protected Connection(UdpEndPoint remoteEndPoint, ChannelSettings channelSettings)
        {
            RemoteEndPoint = remoteEndPoint;

            State = ConnectionState.Disconnected;

            _channels = new ChannelCollection(channelSettings);
        }


        public int MTU => 508;

        public int RTT => _rtt;

        public ConnectionState State
        {
            get => _state;
            protected set => _state = value;
        }

        public UdpEndPoint RemoteEndPoint { get; }


        public event TypedEventHandler<Connection>? Disconnected;

        public event TypedEventHandler<IConnectionChannel, byte[]>? MessageReceived;


        public void Update()
        {
            if (State != ConnectionState.Connected)
            {
                return;
            }

            ProcessFragments();

            ProcessChannels();

            ProcessPing();
        }

        private void ProcessFragments()
        {
            lock (_fragmentLock)
            {
                var now = Timestamp.GetCurrent();
                var groups = _fragmentGroups.Values
                    .Where(x => x.Timestamp + _fragmentTimeout < now)
                    .ToList();
                foreach (var group in groups)
                {
                    group.Return();
                }
            }
        }

        private void ProcessChannels()
        {
            foreach (var channel in _channels)
            {
                var receivedMessages = channel.GetReceivedMessages();
                if (receivedMessages?.Count > 0)
                {
                    foreach (var data in receivedMessages)
                    {
                        OnMessageReceived(channel, data);
                    }
                }

                var outgoingPackets = channel.CollectOutgoingPackets();
                if (outgoingPackets?.Count > 0)
                {
                    foreach (var outgoingPacket in outgoingPackets)
                    {
                        SendChannelPacket(channel, outgoingPacket);
                    }
                }
            }
        }

        private void ProcessPing()
        {
            SeqNo? pingSequence = null;
            lock (_pingLock)
            {
                var now = Timestamp.GetCurrent();
                if (_pingTimestamp + _pingInterval < now)
                {
                    _pingTimestamp = now;
                    _pingSequence++;

                    pingSequence = _pingSequence;
                }
            }

            if (pingSequence != null)
            {
                SendPingPacket(pingSequence.Value);
            }
        }


        public abstract void Connect();

        public void SendMessage(byte channelId, byte[] data)
        {
            if (State != ConnectionState.Connected)
            {
                return;
            }

            var channel = _channels.Get(channelId, this);
            channel.SendMessage(data);
        }


        internal void HandleIncomingPacket(UdpPacket packet)
        {
            if (State != ConnectionState.Connected)
            {
                return;
            }

            var reader = packet.Reader;

            if (!CheckIncomingPacketData(reader))
            {
                return;
            }

            var packetType = (PacketType)reader.ReadByte();
            switch (packetType)
            {
            case PacketType.Channel:
                ReceiveChannelPacket(reader);
                break;

            case PacketType.Fragment:
                ReceiveFragmentPacket(reader);
                break;

            case PacketType.Ping:
                ReceivePingPacket(reader);
                break;

            case PacketType.Pong:
                ReceivePongPacket(reader);
                break;

            default:
                // Ignore not supported packet types
                break;
            }
        }

        internal void HandleOutgoingPacket(UdpPacket packet)
        {
            var writer = packet.Writer;

            FinalizeOutgoingPacketData(writer);

            if (writer.Length <= MTU)
            {
                SendPacket(packet);
            }
            else
            {
                SeqNo fragmentSeq;
                lock (_fragmentLock)
                {
                    fragmentSeq = _fragmentSeq++;
                }

                var data = writer.GetReadOnlySpan();
                var fragmentCount = (byte)Math.Ceiling(data.Length / (double)MTU);

                for (byte i = 0; i < fragmentCount; i++)
                {
                    var fragmentLength = (i == fragmentCount - 1 ? data.Length - (i * MTU) : MTU);
                    var fragmentData = data.Slice(i * MTU, fragmentLength);
                    SendFragmentPacket(fragmentSeq, fragmentCount, i, fragmentData);
                }

                // Return the original packet back to packet pool
                packet.Return();
            }
        }

        private void HandleOutgoingFragmentPacket(UdpPacket packet)
        {
            FinalizeOutgoingPacketData(packet.Writer);

            SendPacket(packet);
        }

        private protected abstract void SendPacket(UdpPacket packet);

        private protected abstract UdpPacket RentPacket();

        private bool CheckIncomingPacketData(NetDataReader reader)
        {
            if (!Crc32.Check(VersionHash, reader.GetReadOnlySpan()))
            {
                return false;
            }

            reader.ResetRelative(0, Crc32.HashLength);
            return true;
        }

        private void FinalizeOutgoingPacketData(NetDataWriter writer)
        {
            writer.Flush();
            writer.Seek(0, SeekOrigin.End);

            var hash = Crc32.Append(VersionHash, writer.GetReadOnlySpan());
            writer.WriteCrc32Hash(hash);
            writer.Flush();
        }


        protected virtual void OnDisconnected()
        {
            Disconnected?.Invoke(this);
        }

        protected virtual void OnMessageReceived(IConnectionChannel channel, byte[] data)
        {
            MessageReceived?.Invoke(channel, data);
        }


        private void ReceiveChannelPacket(NetDataReader reader)
        {
            var channelId = reader.ReadByte();
            if (_channels.TryGet(channelId, this, out var channel))
            {
                channel.HandleIncomingPacket(reader);
            }
        }

        private void SendChannelPacket(Channel channel, ChannelPacket channelPacket)
        {
            var packet = RentPacket();

            packet.Writer.WriteByte((byte)PacketType.Channel);
            packet.Writer.WriteByte(channel.Id);
            channelPacket.SerializeHeader(packet.Writer);
            channelPacket.SerializeData(packet.Writer);

            HandleOutgoingPacket(packet);
        }


        private void ReceiveFragmentPacket(NetDataReader reader)
        {
            UdpPacket? fragmentedPacket = null;
            try
            {
                var fragmentSeq = reader.ReadSeqNo();
                var fragmentCount = reader.ReadByte();

                lock (_fragmentLock)
                {
                    var group = GetFragmentGroup(fragmentSeq, fragmentCount);

                    var fragmentIndex = reader.ReadByte();
                    if (group.CanAdd(fragmentIndex))
                    {
                        AddFragment(group, fragmentIndex, reader.ReadSpan());

                        if (group.IsComplete)
                        {
                            fragmentedPacket = UnpackFragmentGroup(group);
                        }
                    }
                }

                // Handle fragmented packet outside lock statement
                if (fragmentedPacket != null)
                {
                    HandleIncomingPacket(fragmentedPacket);
                }
            }
            finally
            {
                fragmentedPacket?.Return();
            }
        }

        private void SendFragmentPacket(SeqNo fragmentSeq, byte fragmentCount, byte fragmentIndex, ReadOnlySpan<byte> fragmentData)
        {
            var packet = RentPacket();

            packet.Writer.WriteByte((byte)PacketType.Fragment);
            packet.Writer.WriteSeqNo(fragmentSeq);
            packet.Writer.WriteByte(fragmentCount);
            packet.Writer.WriteByte(fragmentIndex);
            packet.Writer.WriteSpan(fragmentData);

            HandleOutgoingFragmentPacket(packet);
        }


        private void ReceivePingPacket(NetDataReader reader)
        {
            var pingSequence = reader.ReadSeqNo();

            SendPongPacket(pingSequence);
        }

        private void SendPingPacket(SeqNo pingSequence)
        {
            var packet = RentPacket();

            packet.Writer.WriteByte((byte)PacketType.Ping);
            packet.Writer.WriteSeqNo(pingSequence);

            HandleOutgoingPacket(packet);
        }


        private void ReceivePongPacket(NetDataReader reader)
        {
            var pingSequence = reader.ReadSeqNo();

            lock (_pingLock)
            {
                if (_pingSequence == pingSequence)
                {
                    _rtt = (int)(Timestamp.GetCurrent() - _pingTimestamp);
                }
            }
        }

        private void SendPongPacket(SeqNo pingSequence)
        {
            var packet = RentPacket();

            packet.Writer.WriteByte((byte)PacketType.Pong);
            packet.Writer.WriteSeqNo(pingSequence);

            HandleOutgoingPacket(packet);
        }


        private FragmentGroup GetFragmentGroup(SeqNo fragmentSeq, byte fragmentCount)
        {
            if (!_fragmentGroups.TryGetValue(fragmentSeq, out var group))
            {
                group = _fragmentGroupPool.Rent();
                group.Timestamp = Timestamp.GetCurrent();
                group.Seq = fragmentSeq;
                group.Count = fragmentCount;
                group.AttachTo(_fragmentGroups);
            }
            return group;
        }

        private UdpPacket UnpackFragmentGroup(FragmentGroup group)
        {
            var packet = RentPacket();

            group.WriteTo(packet.Writer);
            group.Return();

            packet.Reader.Reset(packet.Writer.Length);

            return packet;
        }

        private void AddFragment(FragmentGroup group, byte fragmentIndex, ReadOnlySpan<byte> fragmentData)
        {
            var fragment = _fragmentPool.Rent();

            fragment.Set(fragmentIndex, fragmentData);

            group.Add(fragment);
        }


        private int _disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 1)
            {
                return;
            }

            if (disposing)
            {
                lock (_fragmentLock)
                {
                    _fragmentGroupPool.Dispose();
                    _fragmentPool.Dispose();
                    foreach (var group in _fragmentGroups.Values.ToList())
                    {
                        group.Return();
                    }
                }
            }
        }
    }
}
