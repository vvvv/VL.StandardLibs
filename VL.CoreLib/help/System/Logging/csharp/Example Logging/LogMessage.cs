using Microsoft.Extensions.Logging;

namespace VL.System.Logging
{
    public record struct LogMessage(LogLevel LogLevel,
            EventId EventId,
            object State,
            Exception? Exception,
            Func<object, Exception?, string> Formatter)
    {
        public override string ToString() => Formatter(State, Exception);
    }
}