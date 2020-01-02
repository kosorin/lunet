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

        private readonly object _fragmentLock = new object();
        private readonly ObjectPool<FragmentGroup> _fragmentGroupPool = new ObjectPool<FragmentGroup>();
        private readonly ObjectPool<Fragment> _fragmentPool = new ObjectPool<Fragment>();
        private readonly Dictionary<SeqNo, FragmentGroup> _fragmentGroups = new Dictionary<SeqNo, FragmentGroup>();
        private SeqNo _fragmentSeq = SeqNo.Zero;

        private readonly object _pingLock = new object();
        private SeqNo _pingSequence;
        private long _pingTimestamp = Timestamp.GetCurrent();
        private long _keepAliveTimestamp = Timestamp.GetCurrent();
        private int _rtt = 500;

        protected Connection(UdpEndPoint remoteEndPoint, ChannelSettings channelSettings)
        {
            _channels = new ChannelCollection(channelSettings);

            State = ConnectionState.Disconnected;
            RemoteEndPoint = remoteEndPoint;
        }


        public int Timeout => 8_000;

        public int PingInterval => 1_000;

        public int RTT => _rtt;

        public int MTU => 508;


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
                throw new InvalidOperationException("Connection is disconnected.");
            }

            ProcessFragments();

            ProcessPing();

            ProcessChannels();
        }

        private void ProcessFragments()
        {
            lock (_fragmentLock)
            {
                var now = Timestamp.GetCurrent();
                foreach (var group in _fragmentGroups.Values)
                {
                    if (group.Timestamp + Timeout < now)
                    {
                        group.Return();
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

                if (_keepAliveTimestamp + Timeout < now)
                {
                    throw new Exception("Connection timeout.");
                }

                if (_pingTimestamp + PingInterval < now)
                {
                    _pingTimestamp = now;

                    pingSequence = _pingSequence++;
                }
            }

            if (pingSequence != null)
            {
                SendPingPacket(pingSequence.Value);
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


        public abstract void Connect();

        public void SendMessage(byte channelId, byte[] data)
        {
            if (State != ConnectionState.Connected)
            {
                throw new InvalidOperationException("Connection is disconnected.");
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
                return;
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


        private void SendChannelPacket(Channel channel, ChannelPacket channelPacket)
        {
            var packet = RentPacket();

            packet.Writer.WriteByte((byte)PacketType.Channel);
            packet.Writer.WriteByte(channel.Id);
            channelPacket.SerializeHeader(packet.Writer);
            channelPacket.SerializeData(packet.Writer);

            HandleOutgoingPacket(packet);
        }

        private void ReceiveChannelPacket(NetDataReader reader)
        {
            var channelId = reader.ReadByte();
            if (_channels.TryGet(channelId, this, out var channel))
            {
                channel.HandleIncomingPacket(reader);
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


        private void SendPingPacket(SeqNo pingSequence)
        {
            var packet = RentPacket();

            packet.Writer.WriteByte((byte)PacketType.Ping);
            packet.Writer.WriteSeqNo(pingSequence);

            HandleOutgoingPacket(packet);
        }

        private void ReceivePingPacket(NetDataReader reader)
        {
            lock (_pingLock)
            {
                var now = Timestamp.GetCurrent();

                _keepAliveTimestamp = now;
            }

            var pingSequence = reader.ReadSeqNo();

            SendPongPacket(pingSequence);
        }


        private void SendPongPacket(SeqNo pingSequence)
        {
            var packet = RentPacket();

            packet.Writer.WriteByte((byte)PacketType.Pong);
            packet.Writer.WriteSeqNo(pingSequence);

            HandleOutgoingPacket(packet);
        }

        private void ReceivePongPacket(NetDataReader reader)
        {
            var pingSequence = reader.ReadSeqNo();

            lock (_pingLock)
            {
                var now = Timestamp.GetCurrent();

                _keepAliveTimestamp = now;

                if (_pingSequence == pingSequence)
                {
                    _rtt = (int)(now - _pingTimestamp);
                }
            }
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
