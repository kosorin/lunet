using System;

namespace Lure.Net
{
    internal struct SequenceNumber
    {
        private const int Range = ushort.MaxValue + 1;
        private const int CompareValue = Range / 2;

        public SequenceNumber(ushort value)
        {
            Value = value;
        }

        public SequenceNumber(int value)
        {
            Value = GetValueFromInt(value);
        }

        public ushort Value { get; }

        /// <summary>
        /// Gets next number in a sequence.
        /// </summary>
        /// <returns></returns>
        public SequenceNumber GetNext()
        {
            return new SequenceNumber(Value + 1);
        }

        public int GetDifference(SequenceNumber other)
        {
            if (Value == other.Value)
            {
                return 0;
            }

            if (IsGreaterThan(Value, other.Value))
            {
                return GetDifference(Value, other.Value);
            }
            else
            {
                return -GetDifference(other.Value, Value);
            }
        }

        public override string ToString()
        {
            return Value.ToString();
        }


        public static bool operator >(SequenceNumber left, SequenceNumber right)
        {
            return IsGreaterThan(left.Value, right.Value);
        }

        public static bool operator <(SequenceNumber left, SequenceNumber right)
        {
            return IsGreaterThan(right.Value, left.Value);
        }

        public static bool operator >=(SequenceNumber left, SequenceNumber right)
        {
            return left.Value == right.Value
                || IsGreaterThan(left.Value, right.Value);
        }

        public static bool operator <=(SequenceNumber left, SequenceNumber right)
        {
            return left.Value == right.Value
                || IsGreaterThan(right.Value, left.Value);
        }

        /// <summary>
        /// Checks whether <paramref name="greater"/> parameter is greater than <paramref name="value"/>.
        /// </summary>
        /// <param name="greater"></param>
        /// <param name="value"></param>
        private static bool IsGreaterThan(ushort greater, ushort value)
        {
            return (value < greater && greater - value <= CompareValue)
                || (greater < value && value - greater > CompareValue);
        }

        /// <summary>
        /// Gets a difference of two sequence numbers.
        /// </summary>
        /// <param name="greater">Must be greater than <see cref="value"/>.</param>
        /// <param name="value"></param>
        private static int GetDifference(ushort greater, ushort value)
        {
            if (greater < value)
            {
                return (greater + Range) - value;
            }
            else
            {
                return greater - value;
            }
        }

        private static ushort GetValueFromInt(int value)
        {
            return (ushort)(value % Range);
        }
    }
}
