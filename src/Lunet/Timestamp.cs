using System;
using System.Diagnostics;

namespace Lunet
{
    public static class Timestamp
    {
        public static long GetCurrent() => Stopwatch.GetTimestamp() / TimeSpan.TicksPerMillisecond;
    }
}
