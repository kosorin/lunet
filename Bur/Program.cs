using Bur.Common.Extensions;
using Bur.Net;
using Serilog;
using System.Diagnostics;
using System.Text;

namespace Bur
{
    internal static class Program
    {
        private static readonly ILogger Logger = Log.ForContext(typeof(Program));

        private static void Main()
        {
            Logging.Initialize();
        }
    }
}
