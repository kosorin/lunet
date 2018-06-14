using Lure.Net.Packets;
using System;
using System.Diagnostics;

namespace Lure.Net
{
    [DebuggerDisplay("{Value,nq}")]
    internal struct SeqNo : IEquatable<SeqNo>, IPacketPart
    {
        private const int Range = ushort.MaxValue + 1;

        private const int CompareValue = Range / 2;

        private readonly ushort _value;

        public SeqNo(ushort value)
        {
            _value = value;
        }

        public SeqNo(int value)
        {
            _value = GetValueFromInt(value);
        }

        public static SeqNo Zero { get; } = new SeqNo(0);

        public ushort Value => _value;

        public int Length => sizeof(ushort);


        public int GetDifference(SeqNo other)
        {
            if (_value == other._value)
            {
                return 0;
            }

            if (IsGreaterThan(_value, other._value))
            {
                return GetDifference(_value, other._value);
            }
            else
            {
                return -GetDifference(other._value, _value);
            }
        }

        public bool Equals(SeqNo other)
        {
            return _value == other._value;
        }

        public override bool Equals(object obj)
        {
            return obj is SeqNo other && _value == other._value;
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return _value.ToString();
        }

        #region Operators

        public static SeqNo operator ++(SeqNo seq)
        {
            return new SeqNo(seq._value + 1);
        }

        public static SeqNo operator --(SeqNo seq)
        {
            return new SeqNo(seq._value - 1);
        }

        public static SeqNo operator +(SeqNo seq, int value)
        {
            return new SeqNo(seq._value + value);
        }

        public static SeqNo operator -(SeqNo seq, int value)
        {
            return new SeqNo(seq._value - value);
        }

        public static bool operator >(SeqNo left, SeqNo right)
        {
            return IsGreaterThan(left._value, right._value);
        }

        public static bool operator <(SeqNo left, SeqNo right)
        {
            return IsGreaterThan(right._value, left._value);
        }

        public static bool operator >=(SeqNo left, SeqNo right)
        {
            return left._value == right._value
                || IsGreaterThan(left._value, right._value);
        }

        public static bool operator <=(SeqNo left, SeqNo right)
        {
            return left._value == right._value
                || IsGreaterThan(right._value, left._value);
        }

        public static bool operator ==(SeqNo left, SeqNo right)
        {
            return left._value == right._value;
        }

        public static bool operator !=(SeqNo left, SeqNo right)
        {
            return left._value != right._value;
        }

        #endregion Operators

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
