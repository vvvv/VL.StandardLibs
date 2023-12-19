#nullable enable
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace VL.Core.Logging
{
    public sealed class LogRecorder : IDisposable
    {
        private readonly ConcurrentQueue<LogMessage> _messages = new();
        private readonly int[] _counts = new int[(int)LogLevel.None];

        private readonly IDisposable? _onChangeToken;

        private int _totalCount;
        private LogRecorderOptions _config;
        private LogMessage? _lastMessage;

        public LogRecorder(IOptionsMonitor<LogRecorderOptions> config)
        {
            _config = config.CurrentValue;
            _onChangeToken = config.OnChange(c => _config = c);
        }

        public LogRecorderOptions Options => _config;

        public IReadOnlyCollection<LogMessage> Messages => _messages;

        public int TotalCount => _totalCount;

        public int GetCount(LogLevel level) => _counts[(int)level];

        public void Clear()
        {
            _messages.Clear();
            Array.Clear(_counts);
            _totalCount = 0;
        }

        internal bool IsEnabled(LogLevel level) => level >= _config.Threshold;

        internal void Record<TState>(
            LogSource source,
            string category,
            NodePath nodePath,
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var logEntry = new LogEntry(source, category, nodePath, logLevel, eventId, formatter(state, exception), exception?.ToString());

            if (_lastMessage?.LogEntry == logEntry)
            {
                _lastMessage.Increment();
            }
            else
            {
                var message = new LogMessage(logEntry, DateTime.Now);
                _lastMessage = message;

                while (_messages.Count >= _config.Capacity)
                    _messages.TryDequeue(out _);

                _messages.Enqueue(message);
            }

            Interlocked.Increment(ref _counts[(int)logLevel]);
            Interlocked.Increment(ref _totalCount);
        }

        void IDisposable.Dispose()
        {
            Clear();
            _onChangeToken?.Dispose();
        }
    }
}