using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using VL.Core;
using VL.Core.Logging;

namespace VL.IO.Redis
{
    /// <summary>
    /// Sets up a connection to a database on a Redis server
    /// </summary>
    internal sealed class RedisConnectionManager : IDisposable
    {
        private readonly NodeContext _nodeContext;
        private readonly ILogger _logger;

        private CancellationTokenSource? _connectCancellationTokenSource;
        private Task? _connectTask;
        private bool _disposed;

        private string? _configuration;
        private readonly BehaviorSubject<RedisConnection?> _redisConnection = new(null);

        public RedisConnectionManager(NodeContext nodeContext)
        {
            _nodeContext = nodeContext;
            _logger = nodeContext.GetLogger();
        }

        public void Dispose()
        {
            _disposed = true;

            _connectCancellationTokenSource?.Cancel();
            _connectCancellationTokenSource?.Dispose();
            _connectCancellationTokenSource = null;

            _redisConnection.Value?.Dispose();
            _redisConnection.Dispose();
        }

        public RedisConnection? Update(string? configuration, Action<ConfigurationOptions>? configure, bool connectAsync, RedisClient module, ClientSideCachingOptions cachingOptions)
        {
            if (configuration != _configuration)
            {
                _redisConnection.Value?.Dispose();
                _redisConnection.OnNext(null);

                _configuration = configuration;

                if (_connectCancellationTokenSource != null && _connectTask != null)
                {
                    _connectCancellationTokenSource.Cancel();
                    _connectCancellationTokenSource.Dispose();
                }

                _connectCancellationTokenSource = new CancellationTokenSource();
                _connectTask = Reconnect(configuration, configure, connectAsync, module, _connectCancellationTokenSource.Token, cachingOptions, _connectTask);
            }

            return _redisConnection.Value;
        }

        public bool IsConnected => CurrentConnection != null && CurrentConnection.Multiplexer.IsConnected;

        public string ClientName => CurrentConnection?.ClientName ?? string.Empty;

        public ConfigurationOptions? Options { get; set; }

        public IObservable<RedisConnection?> ConnectionObservable => _redisConnection;
        public RedisConnection? CurrentConnection => _redisConnection.Value;

        private static LogLevel DemoteRedisInformationToDebug(string categoryName, LogLevel level)
            => level == LogLevel.Information && categoryName.StartsWith("StackExchange.Redis", StringComparison.Ordinal)
                ? LogLevel.Debug
                : level;

        private async Task Reconnect(string? configuration, Action<ConfigurationOptions>? configure, bool connectAsync, RedisClient client, CancellationToken cancellationToken, ClientSideCachingOptions cachingOptions, Task? existingConnectTask)
        {
            // Reset options while reconnecting - they are used for display in tooltip
            Options = default;

            var options = new ConfigurationOptions();
            if (configuration != null)
                options = ConfigurationOptions.Parse(configuration);
            if (configure != null)
                options.Apply(configure);

            // StackExchange.Redis logs the whole connection handshake on Information level. Keep that for troubleshooting, but on Debug.
            options.LoggerFactory ??= _nodeContext.AppHost.LoggerFactory.WithLogLevelMapping(DemoteRedisInformationToDebug);
            options.Protocol = RedisProtocol.Resp2;
            // Attach our unique id so we can identify our pub/sub connection later (see below)
            options.ClientName = $"{options.ClientName ?? options.Defaults.ClientName}{GetHashCode()}";
            // Needed to get the client list, see comment in RedisClient
            options.AllowAdmin = true;

            // Set initial retry count to 0, as we manage that ourselves in the reconnect loop (gives use more control)
            options.ConnectRetry = 0;
            // Don't queue commands while disconnected
            options.BacklogPolicy = BacklogPolicy.FailFast;

            var endPoints = string.Join(", ", options.EndPoints);
            _logger.LogInformation("Connecting to Redis server {EndPoints}", endPoints);

            _connectCancellationTokenSource = new CancellationTokenSource();
            var token = _connectCancellationTokenSource.Token;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (existingConnectTask != null)
                        await existingConnectTask;

                    var multiplexer = connectAsync
                        ? await ConnectionMultiplexer.ConnectAsync(options)
                        : ConnectionMultiplexer.Connect(options);

                    if (!_disposed && !token.IsCancellationRequested)
                    {
                        _logger.LogInformation("Connected to Redis server {EndPoints}", endPoints);
                        _redisConnection.OnNext(new RedisConnection(multiplexer, _logger, client, cachingOptions));
                    }
                    else
                        multiplexer.Dispose();

                    break;
                }
                catch (Exception)
                {
                    if (!token.IsCancellationRequested)
                    {
                        // Exception was already logged by the library, no need to do so again
                        await Task.Delay(1000, token);
                    }
                }
            }

            Options = options;
        }
    }
}
