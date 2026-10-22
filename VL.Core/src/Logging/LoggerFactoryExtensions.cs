#nullable enable
using Microsoft.Extensions.Logging;
using System;

namespace VL.Core.Logging
{
    /// <summary>
    /// Extensions for changing the severity of logged messages.
    /// </summary>
    public static class LoggerFactoryExtensions
    {
        /// <summary>
        /// Microsoft.Extensions.Logging can filter messages by category and level, but it cannot promote or demote them.
        /// This wraps the factory in one which changes the level of messages before they are handed to the original factory.
        /// Loggers of the returned factory are enabled according to the mapped level.
        /// The original factory is shared and therefore not disposed when the returned factory is disposed.
        /// </summary>
        /// <param name="loggerFactory">The factory to wrap.</param>
        /// <param name="levelMapper">Receives the category of the logger and the level a message was logged with and returns the level it should be logged with instead. Return the given level to leave a message untouched.</param>
        /// <returns>A factory which remaps the level of the messages logged through it.</returns>
        public static ILoggerFactory WithLogLevelMapping(this ILoggerFactory loggerFactory, Func<string, LogLevel, LogLevel> levelMapper)
        {
            if (loggerFactory is null)
                throw new ArgumentNullException(nameof(loggerFactory));
            if (levelMapper is null)
                throw new ArgumentNullException(nameof(levelMapper));
            return new LogLevelMappingLoggerFactory(loggerFactory, levelMapper);
        }
    }

    sealed class LogLevelMappingLoggerFactory : ILoggerFactory
    {
        private readonly ILoggerFactory _inner;
        private readonly Func<string, LogLevel, LogLevel> _mapper;

        public LogLevelMappingLoggerFactory(ILoggerFactory inner, Func<string, LogLevel, LogLevel> mapper)
        {
            _inner = inner;
            _mapper = mapper;
        }

        public void AddProvider(ILoggerProvider provider) => _inner.AddProvider(provider);

        public ILogger CreateLogger(string categoryName)
            => new LogLevelMappingLogger(_inner.CreateLogger(categoryName), categoryName, _mapper);

        public void Dispose()
        {
            // Not owned
        }

        sealed class LogLevelMappingLogger : ILogger
        {
            private readonly ILogger _inner;
            private readonly string _categoryName;
            private readonly Func<string, LogLevel, LogLevel> _mapper;

            public LogLevelMappingLogger(ILogger inner, string categoryName, Func<string, LogLevel, LogLevel> mapper)
            {
                _inner = inner;
                _categoryName = categoryName;
                _mapper = mapper;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull
                => _inner.BeginScope(state);

            public bool IsEnabled(LogLevel logLevel)
                => _inner.IsEnabled(_mapper(_categoryName, logLevel));

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                => _inner.Log(_mapper(_categoryName, logLevel), eventId, state, exception, formatter);
        }
    }
}
