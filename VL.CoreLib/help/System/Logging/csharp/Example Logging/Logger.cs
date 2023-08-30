using Microsoft.Extensions.Logging;

namespace VL.System.Logging
{
    sealed class VLLogger : ILogger
    {
        private readonly LogMessageBroker _provider;
        private readonly string _name;
        private readonly Func<VLLoggerConfiguration> _getCurrentConfig;

        public VLLogger(string name, LogMessageBroker provider)
        {
            _provider = provider;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;

        public bool IsEnabled(LogLevel logLevel) =>
            _getCurrentConfig().LogLevelToColorMap.ContainsKey(logLevel);

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            //if (!IsEnabled(logLevel))
            //{
            //    return;
            //}

            _provider.SendLogMessage(this, logLevel, eventId, state, exception, formatter);

            //VLLoggerConfiguration config = _getCurrentConfig();
            //if (config.EventId == 0 || config.EventId == eventId.Id)
            //{
            //    ConsoleColor originalColor = Console.ForegroundColor;

            //    Console.ForegroundColor = config.LogLevelToColorMap[logLevel];
            //    Console.WriteLine($"[{eventId.Id,2}: {logLevel,-12}]");

            //    Console.ForegroundColor = originalColor;
            //    Console.Write($"     {_name} - ");

            //    Console.ForegroundColor = config.LogLevelToColorMap[logLevel];
            //    Console.Write($"{formatter(state, exception)}");

            //    Console.ForegroundColor = originalColor;
            //    Console.WriteLine();
            //}
        }
    }
}