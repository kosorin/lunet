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

        private static readonly Dictionary<Type, PacketDataType> DataTypes = new Dictionary<Type, PacketDataType>();

        private bool _disposed;

        private readonly Dictionary<PacketDataType, ObjectPool<PacketData>> _pools = new Dictionary<PacketDataType, ObjectPool<PacketData>>();

        static PacketDataPool()
        {
            var packetTypes = typeof(PacketDataPool).Assembly
                .GetTypes()
                .Select(x => (Attribute: x.GetCustomAttribute<PacketDataAttribute>(false), Type: x))
                .Where(x => x.Attribute != null && typeof(PacketData).IsAssignableFrom(x.Type))
                .Select(x => (DataType: x.Attribute.DataType, ClassType: x.Type))
                .ToList();

            foreach (var (dataType, classType) in packetTypes)
            {
                ClassTypes.Add(dataType, classType);
                DataTypes.Add(classType, dataType);
            }
        }

        public TPacketData Rent<TPacketData>() where TPacketData : PacketData
        {
            var type = DataTypes[typeof(TPacketData)];
            return (TPacketData)GetPool(type).Rent();
        }

        public PacketData Rent(PacketDataType dataType)
        {
            return GetPool(dataType).Rent();
        }

        public void Return(PacketData data)
        {
            if (data == null)
            {
                return;
            }

            var type = DataTypes[data.GetType()];
            GetPool(type).Return(data);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private ObjectPool<PacketData> GetPool(PacketDataType dataType)
        {
            if (_pools.TryGetValue(dataType, out var pool))
            {
                return pool;
            }
            else
            {
                return AddPool(dataType);
            }
        }

        private ObjectPool<PacketData> AddPool(PacketDataType dataType)
        {
            var activator = CreateActivator(dataType);
            if (activator != null)
            {
                var pool = new ObjectPool<PacketData>(() => activator());
                _pools[dataType] = pool;
                return pool;
            }
            else
            {
                throw new Exception($"Could not create a packet activator: {dataType}.");
            }
        }

        private ObjectActivator<PacketData> CreateActivator(PacketDataType dataType)
        {
            if (ClassTypes.TryGetValue(dataType, out var classType))
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
