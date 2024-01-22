#nullable enable
using Microsoft.Extensions.Logging;
using System;

namespace VL.Core.Logging
{
    public sealed record LogRecorderOptions
    {
        private int capacity = 8192;

        /// <summary>
        /// The amount of message the recorder can hold. Once the capacity is reached oldest messages will be removed.
        /// </summary>
        public int Capacity
        {
            get => capacity;
            set => capacity = Math.Clamp(value, 1, int.MaxValue);
        }

        /// <summary>
        /// The minimum log level for a log message to get recorded. Defaults to <see cref="LogLevel.Information"/>.
        /// </summary>
        public LogLevel Threshold { get; set; } = LogLevel.Information;
    }
}