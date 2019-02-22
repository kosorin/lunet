using System;
using System.Diagnostics;

namespace Lure
{
    public static class Timestamp
    {
        public static long Current => Stopwatch.GetTimestamp() / TimeSpan.TicksPerMillisecond;
    }
}
