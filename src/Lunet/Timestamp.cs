using System;
using System.Diagnostics;

namespace Lunet
{
    public static class Timestamp
    {
        public static long Current => Stopwatch.GetTimestamp() / TimeSpan.TicksPerMillisecond;
    }
}
