using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using VL.Lib.Collections;

namespace VL.System.Logging
{
    public static class VLLoggerExtensions
    {
        public static ILoggingBuilder AddVLLogger(
            this ILoggingBuilder builder, List<string> entries)
        {
            builder.AddConfiguration();

            builder.Services.TryAddEnumerable(
                ServiceDescriptor.Singleton<ILoggerProvider, LogMessageBroker>());

            LoggerProviderOptions.RegisterProviderOptions
                <VLLoggerConfiguration, LogMessageBroker>(builder.Services);

            return builder;
        }

        public static ILoggingBuilder AddVLLogger(
            this ILoggingBuilder builder,
            Action<VLLoggerConfiguration> configure, List<string> entries)
        {
            builder.AddVLLogger(entries);
            builder.Services.Configure(configure);

            return builder;
        }

        public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, string message, Spread<object> args)
        {
            if (logger.IsEnabled(logLevel))
                logger.Log(logLevel, eventId, message, args.GetInternalArray());
        }
    }
}
