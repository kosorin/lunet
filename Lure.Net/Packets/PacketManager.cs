using Lure.Net.Data;
using Lure.Net.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Lure.Net.Packets
{
    internal sealed class PacketManager
    {
        private static readonly Dictionary<PacketType, Type> ClassTypes = new Dictionary<PacketType, Type>();
        private static readonly Dictionary<Type, PacketType> PacketTypes = new Dictionary<Type, PacketType>();

        private readonly Dictionary<PacketType, ObjectPool<Packet>> _pools = new Dictionary<PacketType, ObjectPool<Packet>>();

        static PacketManager()
        {
            var packetTypes = typeof(PacketManager).Assembly
                .GetTypes()
                .Select(x => (Attribute: x.GetCustomAttribute<PacketAttribute>(false), Type: x))
                .Where(x => x.Attribute != null && typeof(Packet).IsAssignableFrom(x.Type))
                .Select(x => (PacketType: x.Attribute.Type, Type: x.Type))
                .ToList();

            foreach (var (packetType, classType) in packetTypes)
            {
                ClassTypes.Add(packetType, classType);
                PacketTypes.Add(classType, packetType);
            }
        }

        public TPacket Create<TPacket>() where TPacket : Packet
        {
            if (PacketTypes.TryGetValue(typeof(TPacket), out var type))
            {
                return (TPacket)GetPool(type)?.Rent();
            }
            else
            {
                return null;
            }
        }

        public Packet Parse(INetDataReader reader)
        {
            var type = (PacketType)reader.ReadByte();
            var packet = GetPool(type)?.Rent();
            if (packet != null)
            {
                reader.ReadSerializable(packet);
            }
            return packet;
        }

        public void Release(Packet packet)
        {
            if (packet == null)
            {
                return;
            }

            GetPool(packet.Type)?.Return(packet);
        }

        public void Dispose()
        {
            foreach (var pool in _pools.Values)
            {
                pool.Dispose();
            }
        }

        private ObjectPool<Packet> GetPool(PacketType type)
        {
            if (_pools.TryGetValue(type, out var pool))
            {
                return pool;
            }
            else
            {
                return AddPool(type);
            }
        }

        private ObjectPool<Packet> AddPool(PacketType type)
        {
            var factory = CreatePacketFactory(type);
            if (factory != null)
            {
                var pool = new ObjectPool<Packet>(factory);
                _pools[type] = pool;
                return pool;
            }
            else
            {
                throw new Exception($"Could not create factory for packet type: {type}.");
            }
        }

        private Func<Packet> CreatePacketFactory(PacketType type)
        {
            if (ClassTypes.TryGetValue(type, out var classType))
            {
                return () => (Packet)Activator.CreateInstance(classType);
            }
            else
            {
                return null;
            }
        }
    }
}
