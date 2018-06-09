using System.Threading;

namespace Lure.Net
{
    internal class SequenceNumber
    {
        private const int MaxValue = ushort.MaxValue + 1;
        private const int MaxCompareValue = MaxValue / 2;

        private volatile int _value;

        public ushort Value => (ushort)(_value % MaxValue);

        public ushort GetNext()
        {
            return (ushort)(Interlocked.Increment(ref _value) % MaxValue);
        }

        public bool GreaterThan(ushort other)
        {
            return GreaterThan(Value, other);
        }

        public bool LessThan(ushort other)
        {
            return GreaterThan(other, Value);
        }

        private bool GreaterThan(ushort left, ushort right)
        {
            return (left > right && left - right <= MaxCompareValue)
                || (left < right && right - left > MaxCompareValue);
        }
    }
}
