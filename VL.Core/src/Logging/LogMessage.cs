#nullable enable
using Microsoft.Extensions.Logging;
using System;

namespace VL.Core.Logging
{
    public record struct LogMessage(
            AppHost Host,
            NodePath NodePath,
            DateTime DateTime,
            string Category,
            LogLevel LogLevel,
            EventId EventId,
            string Message,
            Exception? Exception)
    {
        public override string ToString() => Message;

        public bool HasNodePath => !NodePath.IsDefault;
    }
}