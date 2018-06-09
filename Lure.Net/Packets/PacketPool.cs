using System;
using System.Collections.Generic;

namespace Lure.Net.Packets
{
    internal sealed class PacketPool : IObjectPool<Packet, PacketType>
    {
        private readonly Dictionary<PacketType, IObjectPool<Packet>> _pools = new Dictionary<PacketType, IObjectPool<Packet>>();

        public Packet Rent(PacketType arg)
        {
            return GetPool(arg)?.Rent();
        }

        public void Return(Packet item)
        {
            if (item == null)
            {
                return;
            }

            GetPool(item.Type)?.Return(item);
        }

        public void Dispose()
        {
            foreach (var pool in _pools.Values)
            {
                pool.Dispose();
            }
        }

        private IObjectPool<Packet> GetPool(PacketType type)
        {
            if (_pools.TryGetValue(type, out var pool))
            {
                return pool;
            }
            else
            {
                var factory = CreatePacketFactory(type);
                if (factory == null)
                {
                    return null;
                }
                pool = new ObjectPool<Packet>(factory);
                _pools[type] = pool;
                return pool;
            }
        }

        private Func<Packet> CreatePacketFactory(PacketType type)
        {
            switch (type)
            {
            case PacketType.Fragment: throw new NotImplementedException();

            case PacketType.Payload: return () => new PayloadPacket();

            case PacketType.Ping: throw new NotImplementedException();

            case PacketType.Pong: throw new NotImplementedException();

            case PacketType.ConnectRequest: throw new NotImplementedException();

            case PacketType.ConnectAccept: throw new NotImplementedException();

            case PacketType.ConnectDeny: throw new NotImplementedException();

            case PacketType.KeepAlive: throw new NotImplementedException();

            case PacketType.Disconnect: throw new NotImplementedException();

            default: return null;
            }
        }
    }
}
