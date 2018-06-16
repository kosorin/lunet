using System;
using System.Diagnostics;

namespace Lure
{
    public static class Timestamp
    {
        public static long Current => Stopwatch.GetTimestamp() / TimeSpan.TicksPerMillisecond;
    }

    internal static class PreciseDateTime
    {
        private static readonly DateTime _timestampOffset = new DateTime(2015, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static readonly TimeSpan _idle = TimeSpan.FromSeconds(60);

        private static long _timestampStart;

        private static DateTime _start;

        private static Stopwatch _stopwatch;

        public static DateTime UtcNow
        {
            get
            {
                var utcNow = DateTime.UtcNow;
                if (_stopwatch == null || _start.Add(_idle) < utcNow)
                {
                    Reset(utcNow);
                }
                return _start.AddTicks(_stopwatch.Elapsed.Ticks);
            }
        }

        private static void Reset(DateTime utcNow)
        {
            _timestampStart = (utcNow - _timestampOffset).Ticks / TimeSpan.TicksPerMillisecond;
            _start = utcNow;
            _stopwatch = Stopwatch.StartNew();
        }
    }
}
