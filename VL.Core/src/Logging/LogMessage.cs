#nullable enable
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading;

namespace VL.Core.Logging
{
    public class LogMessage
    {
        private int _repeatCount;

        internal LogMessage(LogEntry logEntry, DateTime timestamp)
        {
            LogEntry = logEntry;
            Timestamp = timestamp;
        }

        internal LogEntry LogEntry { get; }

        public LogSource Source => LogEntry.Source;

        public string Category => LogEntry.Category;

        public NodePath NodePath => LogEntry.NodePath;

        public LogLevel LogLevel => LogEntry.LogLevel;

        public EventId EventId => LogEntry.EventId;

        public string Message => LogEntry.Message;

        public string? Exception => LogEntry.Exception;

        public DateTime Timestamp { get; private set; }

        public int RepeatCount => _repeatCount;

        /// <summary>
        /// A short version of the original log message containing only the first line.
        /// </summary>
        public string ShortMessage
        {
            get
            {
                var newLineIndex = Message.IndexOf(Environment.NewLine);
                if (newLineIndex > 0)
                    return Message.Substring(0, newLineIndex);

                return Message;
            }
        }

        /// <summary>
        /// The original log message including any exception details (if any).
        /// </summary>
        public string DetailedMessage
        {
            get
            {
                if (Exception is null)
                    return Message;

                var sb = new StringBuilder();
                sb.AppendLine(Message);
                sb.AppendLine();
                sb.AppendLine(Exception);
                return sb.ToString();
            }
        }

        public bool HasNodePath => !NodePath.IsDefault;

        internal void Increment()
        {
            Timestamp = DateTime.Now;
            Interlocked.Increment(ref _repeatCount);
        }

        /// <summary>
        /// yyyy.MM.dd hh:mm:ss.fff [LVL] (Src) Id Category DetailedMessage
        /// </summary>
        public override string ToString() => ToString(timestampFormat: "yyyy/MM/dd HH:mm:ss.fff");

        public string ToString(string timestampFormat)
        {
            return $"{Timestamp.ToString(timestampFormat)} {GetShortLabel(LogLevel)} ({Source}) {EventId} {Category} {DetailedMessage}";

            static string GetShortLabel(LogLevel level) => level switch
            {
                LogLevel.Trace => "[TRC]",
                LogLevel.Debug => "[DBG]",
                LogLevel.Information => "[INF]",
                LogLevel.Warning => "[WRN]",
                LogLevel.Error => "[ERR]",
                LogLevel.Critical => "[CRT]",
                _ => "[   ]"
            };
        }
    }

    internal record struct LogEntry(
        LogSource Source,
        string Category,
        NodePath NodePath,
        LogLevel LogLevel,
        EventId EventId,
        string Message,
        string? Exception);
}