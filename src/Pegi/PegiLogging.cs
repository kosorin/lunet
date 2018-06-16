﻿using Lure;
using Serilog;
using Serilog.Events;
using System.Reflection;

namespace Pegi
{
    public static class PegiLogging
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