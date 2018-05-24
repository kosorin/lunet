using Bur.Common;
using Serilog;
using Serilog.Events;
using System.Reflection;

namespace Pur
{
    public static class PurLogging
    {
        public static void Configure(string name)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Logging.Configure(config => config
                .MinimumLevel.Verbose()
                .WriteTo.Console(LogEventLevel.Verbose, $"{name}: [{{Timestamp:{Logging.ShortTimeFormatString}}} {{Level:u3}}] {{Message:lj}}{{NewLine}}{{Exception}}")
            );
            Log.Logger.Information("{AppName} v{Version}", name, version);
        }
    }
}
