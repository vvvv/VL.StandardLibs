using Microsoft.Extensions.Logging;
using VL.Core;

namespace VL.System.Logging
{
    public record struct LogMessage(
            string Category,
            LogLevel LogLevel,
            EventId EventId,
            object State,
            Exception? Exception,
            Func<object, Exception?, string> Formatter)
    {
        public override string ToString() => Formatter(State, Exception);

        public bool HasNodeContext => State is ILogMessageWithNodeContext;

        public NodeContext NodeContext => State is ILogMessageWithNodeContext c ? c.NodeContext : NodeContext.Default;
    }
}