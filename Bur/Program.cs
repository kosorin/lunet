using Bur.Net;
using Serilog;
using System;
using System.Linq;

namespace Bur
{
    internal static class Program
    {
        private static readonly ILogger logger = Log.ForContext(typeof(Program));

        private static void Main()
        {
            Logging.Initialize();
        }
    }
}
