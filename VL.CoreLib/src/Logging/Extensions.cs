using Microsoft.Extensions.Logging;
using VL.Lib.Collections;

namespace VL.Lib.Logging
{
    public static class VLLoggerExtensions
    {
        public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, string message, Spread<object> args)
        {
            logger.Log(logLevel, eventId, message, args.GetInternalArray());
        }
    }
}
