using Serilog;
using System.Diagnostics;

namespace Lure.Extensions.Diagnostics
{
    public static class StopwatchExtensions
    {
        public static void LogElapsed(this Stopwatch stopwatch, string name = "Stopwatch")
        {
            stopwatch.Stop();
            Log.Debug("{Name} Elapsed [ms]: {ElapsedMilliseconds:N3}", name, stopwatch.ElapsedMilliseconds);
        }
    }
}
