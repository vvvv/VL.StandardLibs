using System.Collections.Concurrent;
using System.Reactive;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;

namespace VL.System.Logging
{
    [ProviderAlias("VLLog")]
    public sealed class LogMessageBroker : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, Logger> _loggers = new(StringComparer.OrdinalIgnoreCase);
        private readonly Subject<LogMessage> _logEvents = new();

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, categoryName => new Logger(categoryName, this));
        }

        public IObservable<LogMessage> LogEvents => _logEvents;

        public void SendLogMessage<TState>(
            string category,
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var message = new LogMessage(category, logLevel, eventId, state!, exception, (s, e) => formatter((TState)s, e));
            _logEvents.OnNext(message);
        }

        public void Dispose()
        {
            _loggers.Clear();
        }

        sealed class Logger : ILogger
        {
            private readonly string _name;
            private readonly LogMessageBroker _broker;

            public Logger(string name, LogMessageBroker broker)
            {
                _name = name;
                _broker = broker;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                _broker.SendLogMessage(_name, logLevel, eventId, state, exception, formatter);
            }
        }
    }
}