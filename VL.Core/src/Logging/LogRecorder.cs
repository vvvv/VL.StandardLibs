#nullable enable
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Threading;
using VL.Lib.Reactive;

namespace VL.Core.Logging
{
    public sealed class LogRecorder : IDisposable
    {
        private readonly ConcurrentQueue<LogMessage> _messages = new();
        private readonly int[] _counts = new int[(int)LogLevel.None];

        private readonly IChannel<LogRecorderOptions> _optionsChannel;
        private readonly IDisposable? _onChangeToken;

        private int _totalCount;
        private bool _overflow;
        private LogMessage? _lastMessage;

        internal LogRecorder(IOptionsMonitor<LogRecorderOptions> config)
        {
            _optionsChannel = Channel.Create(config.CurrentValue);
            _optionsChannel.Validator = x => x is not null ? x : default;
            _onChangeToken = config.OnChange(c => _optionsChannel.EnsureValue(c));
        }

        private LogRecorderOptions CurrentOptions => _optionsChannel.Value!;

        public IChannel<LogRecorderOptions> Options => _optionsChannel;

        public IReadOnlyCollection<LogMessage> Messages => _messages;

        public int TotalCount => _totalCount;

        public bool Overflow => _overflow;

        public int GetCount(LogLevel level) => _counts[(int)level];

        public void Clear()
        {
            _messages.Clear();
            Array.Clear(_counts);
            _totalCount = 0;
            _overflow = false;
            _lastMessage = null;
        }

        internal bool IsEnabled(LogLevel level) => level >= CurrentOptions.Threshold;

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

                while (_messages.Count >= CurrentOptions.Capacity)
                {
                    _messages.TryDequeue(out _);
                    _overflow = true;
                }

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