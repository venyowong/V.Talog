using Serilog;
using Serilog.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using V.Talog.Client;

namespace V.Talog.Extension.Serilog
{
    public static class SinkExtension
    {
        public static LoggerConfiguration Talog(this LoggerSinkConfiguration loggerConfiguration, LogChannel logChannel) 
            => loggerConfiguration.Sink(new TalogSink(logChannel));
    }
}
