using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;

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
    }
}
