using System.Collections.Concurrent;
using System.Reactive;
using System.Reactive.Subjects;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VL.Core;

namespace VL.System.Logging
{
    public sealed class VLLogFactory : ILoggerFactory
    {
        public static VLLogFactory GetForApp(AppHost appHost)
        {
            return appHost.Services.GetOrAddService(_ =>
            {

                return new VLLogFactory();
            });
        }

        private readonly ILoggerFactory _loggerFactory;

        public VLLogFactory()
        {
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddProvider(Broker);
            });
        }

        public LogMessageBroker Broker { get; } = new LogMessageBroker();

        public void AddProvider(ILoggerProvider provider)
        {
            _loggerFactory.AddProvider(provider);
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggerFactory.CreateLogger(categoryName);
        }

        public void Dispose()
        {
            _loggerFactory.Dispose();
        }
    }

    [UnsupportedOSPlatform("browser")]
    [ProviderAlias("VLLog")]
    public sealed class LogMessageBroker : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, VLLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);
        private readonly Subject<EventPattern<ILogger, LogMessage>> _logEvents = new();

        public ILogger CreateLogger(string categoryName) =>
            _loggers.GetOrAdd(categoryName, name => new VLLogger(name, this));

        public IObservable<EventPattern<ILogger, LogMessage>> LogEvents => _logEvents;

        public void SendLogMessage<TState>(
            ILogger logger,
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var message = new LogMessage(logLevel, eventId, state!, exception, (s, e) => formatter((TState)s, e));
            _logEvents.OnNext(new EventPattern<ILogger, LogMessage>(logger, message));
        }

        public void Dispose()
        {
            _loggers.Clear();
        }
    }
}