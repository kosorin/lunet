using Lure.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Lure.Net.Packets
{
    internal sealed class PacketDataPool : IDisposable
    {
        private static readonly Dictionary<PacketDataType, Type> ClassTypes = new Dictionary<PacketDataType, Type>();

        private static readonly Dictionary<Type, PacketDataType> Types = new Dictionary<Type, PacketDataType>();

        private bool _disposed;

        private readonly Dictionary<PacketDataType, ObjectPool<PacketData>> _pools = new Dictionary<PacketDataType, ObjectPool<PacketData>>();

        static PacketDataPool()
        {
            var packetTypes = typeof(PacketDataPool).Assembly
                .GetTypes()
                .Select(x => (Attribute: x.GetCustomAttribute<PacketDataAttribute>(false), Type: x))
                .Where(x => x.Attribute != null && typeof(PacketData).IsAssignableFrom(x.Type))
                .Select(x => (PacketType: x.Attribute.Type, Type: x.Type))
                .ToList();

            foreach (var (packetType, classType) in packetTypes)
            {
                ClassTypes.Add(packetType, classType);
                Types.Add(classType, packetType);
            }
        }

        public TPacketData Rent<TPacketData>() where TPacketData : PacketData
        {
            var type = Types[typeof(TPacketData)];
            return (TPacketData)GetPool(type).Rent();
        }

        public PacketData Rent(PacketDataType type)
        {
            return GetPool(type).Rent();
        }

        public void Return(PacketData data)
        {
            if (data == null)
            {
                return;
            }

            var type = Types[data.GetType()];
            GetPool(type).Return(data);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private ObjectPool<PacketData> GetPool(PacketDataType type)
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

        private ObjectPool<PacketData> AddPool(PacketDataType type)
        {
            var activator = CreateActivator(type);
            if (activator != null)
            {
                var pool = new ObjectPool<PacketData>(() => activator());
                _pools[type] = pool;
                return pool;
            }
            else
            {
                throw new Exception($"Could not create a packet activator: {type}.");
            }
        }

        private ObjectActivator<PacketData> CreateActivator(PacketDataType type)
        {
            if (ClassTypes.TryGetValue(type, out var classType))
            {
                return ObjectActivatorFactory.Create<PacketData>(classType);
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
