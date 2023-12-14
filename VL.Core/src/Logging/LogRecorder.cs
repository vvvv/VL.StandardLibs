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
        private LogRecorderOptions _config;

        public LogRecorder(IOptionsMonitor<LogRecorderOptions> config)
        {
            _config = config.CurrentValue;
            _onChangeToken = config.OnChange(c => _config = c);
        }

        public IReadOnlyCollection<LogMessage> Messages => _messages;

        public int this[LogLevel level] => _counts[(int)level];

        public IReadOnlyList<int> Counts => _counts;

        internal bool IsEnabled(LogLevel level) => level >= _config.Threshold;

        internal void Record(LogMessage message)
        {
            while (_messages.Count >= _config.Capacity)
                _messages.TryDequeue(out _);

            _messages.Enqueue(message);
            Interlocked.Increment(ref _counts[(int)message.LogLevel]);
        }

        public void Clear()
        {
            _messages.Clear();
            Array.Clear(_counts);
        }

        void IDisposable.Dispose()
        {
            Clear();
            _onChangeToken?.Dispose();
        }
    }
}