using Lunet.Common;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Lunet
{
    [DebuggerDisplay("{Value,nq}")]
    public readonly struct SeqNo : IEquatable<SeqNo>, IComparable<SeqNo>
    {
        public const int SizeOf = sizeof(ushort);

        public const int Range = ushort.MaxValue + 1;

        public const int HalfRange = Range / 2;

        public static readonly SeqNo Zero = new SeqNo(0);


        private readonly ushort _value;

        public SeqNo(ushort value)
        {
            _value = value;
        }

        public SeqNo(int value)
        {
            _value = (ushort)(value % Range);
        }

        public ushort Value => _value;

        public void Sort(Span<SeqNo> input, Span<SortItem> outputItems)
        {
            if (input.Length != outputItems.Length)
            {
                throw new InvalidOperationException();
            }

            for (var i = 0; i < outputItems.Length; i++)
            {
                outputItems[i] = new SortItem(i, GetDifference(input[i].Value, Value));
            }


#warning TODO: SeqNo.Sort
            //public bool LessThan(SortItem a, SortItem b)
            //{
            //    return a.Value < b.Value;
            //}
            // TODO: outputItems.WithOrder(new Ordering()).Sort();
        }

        public int CompareTo(SeqNo other)
        {
            return GetDifference(_value, other._value);
        }

        public bool Equals(SeqNo other)
        {
            return _value == other._value;
        }

        public override bool Equals(object? obj)
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
            return GetDifference(left._value, right._value) > 0;
        }

        public static bool operator <(SeqNo left, SeqNo right)
        {
            return GetDifference(left._value, right._value) < 0;
        }

        public static bool operator >=(SeqNo left, SeqNo right)
        {
            return left._value == right._value
                || GetDifference(left._value, right._value) > 0;
        }

        public static bool operator <=(SeqNo left, SeqNo right)
        {
            return left._value == right._value
                || GetDifference(left._value, right._value) < 0;
        }

        public static bool operator ==(SeqNo left, SeqNo right)
        {
            return left._value == right._value;
        }

        public static bool operator !=(SeqNo left, SeqNo right)
        {
            return left._value != right._value;
        }

        public static explicit operator ushort(SeqNo seq)
        {
            return seq._value;
        }

        public static explicit operator int(SeqNo seq)
        {
            return seq._value;
        }

        public static explicit operator SeqNo(ushort value)
        {
            return new SeqNo(value);
        }

        public static explicit operator SeqNo(int value)
        {
            return new SeqNo(value);
        }


        /// <summary>
        /// Gets a difference of two sequence numbers.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetDifference(ushort left, ushort right)
        {
            return -(((right - left + Range + HalfRange) % Range) - HalfRange);
        }
    }
}
