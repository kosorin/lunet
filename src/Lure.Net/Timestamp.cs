using System;
using System.Diagnostics;

namespace Lure.Net
{
    public static class Timestamp
    {
        public static long Current => Stopwatch.GetTimestamp() / TimeSpan.TicksPerMillisecond;
    }
}
