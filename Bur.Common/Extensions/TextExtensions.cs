using System;
using System.Text;

namespace Bur.Common.Extensions
{
    public static class TextExtensions
    {
        private const string HexPrefix = "{";
        private const string HexSuffix = "}";
        private const string BinPrefix = "[";
        private const string BinSuffix = "]";
        private const char Delimiter = '-';

        public static string ToHexString(this byte[] array)
        {
            var sb = new StringBuilder((array.Length * 2) + Math.Max((array.Length / 2) + (array.Length % 2) - 1, 0) + HexPrefix.Length + HexSuffix.Length);
            sb.Append(HexPrefix);
            for (int i = array.Length - 1; i >= 0; i--)
            {
                sb.Append(Convert.ToString(array[i], 16).PadLeft(2, '0'));
                if (i > 0 && (i % 2) == 0)
                {
                    sb.Append(Delimiter);
                }
            }
            sb.Append(HexSuffix);
            return sb.ToString();
        }

        public static string ToBinString(this byte[] array)
        {
            var sb = new StringBuilder((array.Length * 8) + Math.Max(array.Length - 1, 0) + BinPrefix.Length + BinSuffix.Length);
            sb.Append(BinPrefix);
            for (int i = array.Length - 1; i >= 0; i--)
            {
                sb.Append(Convert.ToString(array[i], 2).PadLeft(8, '0'));
                if (i > 0)
                {
                    sb.Append(Delimiter);
                }
            }
            sb.Append(BinSuffix);
            return sb.ToString();
        }

        public static string ToHexString(this byte value)
        {
            var sb = new StringBuilder(2 + HexPrefix.Length + HexSuffix.Length);
            sb.Append(HexPrefix);
            sb.Append(Convert.ToString(value, 16).PadLeft(2, '0'));
            sb.Append(HexSuffix);
            return sb.ToString();
        }

        public static string ToBinString(this byte value)
        {
            var sb = new StringBuilder(2 + BinPrefix.Length + BinSuffix.Length);
            sb.Append(BinPrefix);
            sb.Append(Convert.ToString(value, 2).PadLeft(8, '0'));
            sb.Append(BinSuffix);
            return sb.ToString();
        }
    }
}
