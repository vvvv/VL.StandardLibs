#nullable enable
using Microsoft.Extensions.Logging;
using Stride.Core.Diagnostics;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace VL.Stride.Core
{
    sealed class LogBridge : LogListener
    {
        private readonly ILoggerFactory loggerFactory;
        private readonly ILogger logger;

        private string? lastModule;
        private ILogger? lastLogger;

        public LogBridge(ILoggerFactory loggerFactory, ILogger logger)
        {
            this.loggerFactory = loggerFactory;
            this.logger = logger;
        }

        private ILogger GetLogger(string? module)
        {
            if (module is null)
                return logger;

            if (module == lastModule)
                return lastLogger!;

            lastModule = module;
            return lastLogger = loggerFactory.CreateLogger(module);
        }

        protected override void OnLog(ILogMessage logMessage)
        {
            var logger = GetLogger(logMessage.Module);
            var logLevel = ToLogLevel(logMessage.Type);
            if (logMessage is global::Stride.Core.Diagnostics.LogMessage logMessage2)
                logger.Log(logLevel, logMessage2.Exception, logMessage2.Text);
            else
                logger.Log(logLevel, logMessage.Text);
        }

        static LogLevel ToLogLevel(LogMessageType logMessageType)
        {
            switch (logMessageType)
            {
                case LogMessageType.Fatal:
                    return LogLevel.Critical;
                case LogMessageType.Error:
                    return LogLevel.Error;
                case LogMessageType.Warning:
                    return LogLevel.Warning;
                case LogMessageType.Info:
                    return LogLevel.Information;
                case LogMessageType.Debug:
                    return LogLevel.Debug;
                default:
                    return LogLevel.Trace;
            }
        }
    }
}
