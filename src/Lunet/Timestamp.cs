using System.Diagnostics;

namespace Lunet;

public static class Timestamp
{
    public static long GetCurrent()
    {
        return Stopwatch.GetTimestamp() / TimeSpan.TicksPerMillisecond;
    }
}
