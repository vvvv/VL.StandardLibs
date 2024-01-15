#nullable enable
using Microsoft.Extensions.Logging;
using System;
using VL.Lib.Collections;

namespace VL.Lib.Logging
{
    public static class VLLoggerExtensions
    {
        public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, string message, Spread<object> args, Exception? exception = null)
        {
            logger.Log(logLevel, eventId, exception, message, args.GetInternalArray());
        }
    }
}
