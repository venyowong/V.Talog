using Serilog.Core;
using Serilog.Events;
using System;
using V.Common.Extensions;
using V.Talog.Client;

namespace V.Talog.Extension.Serilog
{
    public class TalogSink : ILogEventSink
    {
        private LogChannel logChannel;

        public TalogSink(LogChannel logChannel)
        {
            this.logChannel = logChannel;
        }

        public void Emit(LogEvent logEvent)
        {
            var log = this.logChannel.Create()
                .Tag("level", logEvent.Level.ToString())
                .Tag("date", logEvent.Timestamp.Date.ToString("yyyyMMdd"));
            if (logEvent.Exception != null)
            {
                log.Tag("exception", logEvent.Exception.GetType().Name);
            }
            log.Log(new
            {
                logEvent.Timestamp,
                logEvent.Level,
                logEvent.MessageTemplate,
                logEvent.Properties,
                Exception = logEvent.Exception?.ToString()
            }.ToJson()).Send();
        }
    }
}
