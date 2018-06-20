using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
