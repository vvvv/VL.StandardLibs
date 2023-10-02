#nullable enable
using Microsoft.Extensions.Logging;
using System;

namespace VL.Core.Logging
{
    public record struct LogMessage(
            AppHost Host,
            NodePath NodePath,
            string Category,
            LogLevel LogLevel,
            EventId EventId,
            object State,
            Exception? Exception,
            Func<object, Exception?, string> Formatter)
    {
        public override string ToString() => Formatter(State, Exception);

        public bool HasNodePath => !NodePath.IsDefault;
    }
}