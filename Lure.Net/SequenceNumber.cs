using Lure.Net.Data;
using System.Threading;

namespace Lure.Net
{
    internal class SequenceNumber : INetSerializable
    {
        private const int Range = ushort.MaxValue + 1;
        private const int CompareValue = Range / 2;

        private volatile int _value;

        public ushort Value => FromInt(_value);

        public static ushort FromInt(int sequence)
        {
            return (ushort)(sequence % Range);
        }

        public static bool GreaterThan(ushort lower, ushort higher)
        {
            return (higher > lower && higher - lower <= CompareValue)
                || (higher < lower && lower - higher > CompareValue);
        }

        public static ushort Difference(ushort lower, ushort higher)
        {
            if (lower > higher)
            {
                return (ushort)((higher + Range) - lower);
            }
            else
            {
                return (ushort)(higher - lower);
            }
        }

        public ushort GetNext()
        {
            return FromInt(Interlocked.Increment(ref _value));
        }

        public bool GreaterThan(ushort lower)
        {
            return GreaterThan(lower, Value);
        }

        public bool LessThan(ushort higher)
        {
            return GreaterThan(Value, higher);
        }

        void INetSerializable.Deserialize(INetDataReader reader)
        {
            _value = reader.ReadUShort();
        }

        void INetSerializable.Serialize(INetDataWriter writer)
        {
            writer.WriteUShort(Value);
        }
    }
}
