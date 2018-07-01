using Lure.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Lure.Net.Packets.System
{
    internal sealed class SystemPacketPool : IDisposable
    {
        private static readonly Dictionary<SystemPacketType, Type> ClassTypes = new Dictionary<SystemPacketType, Type>();
        private static readonly Dictionary<Type, SystemPacketType> PacketTypes = new Dictionary<Type, SystemPacketType>();

        private readonly Dictionary<SystemPacketType, ObjectPool<SystemPacket>> _pools = new Dictionary<SystemPacketType, ObjectPool<SystemPacket>>();

        private bool _disposed;

        static SystemPacketPool()
        {
            var packetTypes = typeof(SystemPacketPool).Assembly
                .GetTypes()
                .Select(x => (Attribute: x.GetCustomAttribute<SystemPacketAttribute>(false), ClassType: x))
                .Where(x => x.Attribute != null && typeof(SystemPacket).IsAssignableFrom(x.ClassType))
                .Select(x => (x.Attribute.PacketType, x.ClassType))
                .ToList();

            foreach (var (packetType, classType) in packetTypes)
            {
                ClassTypes.Add(packetType, classType);
                PacketTypes.Add(classType, packetType);
            }
        }

        public TSystemPacket Rent<TSystemPacket>() where TSystemPacket : SystemPacket
        {
            var type = PacketTypes[typeof(TSystemPacket)];
            return (TSystemPacket)GetPool(type).Rent();
        }

        public SystemPacket Rent(SystemPacketType packetType)
        {
            return GetPool(packetType).Rent();
        }

        public void Return(SystemPacket packet)
        {
            if (packet == null)
            {
                return;
            }

            var type = PacketTypes[packet.GetType()];
            GetPool(type).Return(packet);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private ObjectPool<SystemPacket> GetPool(SystemPacketType packetType)
        {
            if (_pools.TryGetValue(packetType, out var pool))
            {
                return pool;
            }
            else
            {
                return AddPool(packetType);
            }
        }

        private ObjectPool<SystemPacket> AddPool(SystemPacketType packetType)
        {
            var activator = CreateActivator(packetType);
            if (activator != null)
            {
                var pool = new ObjectPool<SystemPacket>(activator);
                pool.ItemCreated += (_, packet) => packet.Type = packetType;
                _pools[packetType] = pool;
                return pool;
            }
            else
            {
                throw new NetException($"Could not create a system packet activator: {packetType}.");
            }
        }

        private ObjectActivator<SystemPacket> CreateActivator(SystemPacketType packetType)
        {
            if (ClassTypes.TryGetValue(packetType, out var classType))
            {
                return ObjectActivatorFactory.Create<SystemPacket>(classType);
            }
            else
            {
                return null;
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    foreach (var pool in _pools.Values)
                    {
                        pool.Dispose();
                    }
                }
                _disposed = true;
            }
        }
    }
}
