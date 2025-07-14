using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using VL.Core;
using VL.Core.Import;
using VL.Lib.Animation;
using VL.Model;

namespace VL.IO.Redis
{
    /// <summary>
    /// Sets up a connection to a database on a Redis server
    /// </summary>
    [ProcessNode(Name = "RedisClient")]
    public sealed class RedisClientManager : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();

        private readonly NodeContext _nodeContext;
        private readonly ILogger _logger;

        private CancellationTokenSource? _connectCancellationTokenSource;
        private Task? _connectTask;
        private bool _disposed;

        private string? _configuration;
        private RedisClient? _redisClient;

        public RedisClientManager(
            [Pin(Visibility = PinVisibility.Hidden)] NodeContext nodeContext,
            [Pin(Visibility = PinVisibility.Hidden)] IFrameClock frameClock)
        {
            _nodeContext = nodeContext;
            _logger = nodeContext.GetLogger();

            frameClock.GetSubFrameEvent(SubFrameEvents.ModulesWriteGlobalChannels)
                .Subscribe(WriteIntoGlobalChannels)
                .DisposeBy(_disposables);

            frameClock.GetSubFrameEvent(SubFrameEvents.ModulesSendingData)
                .Subscribe(SendData)
                .DisposeBy(_disposables);
        }

        public void Dispose()
        {
            _disposed = true;

            _connectCancellationTokenSource?.Cancel();
            _connectCancellationTokenSource?.Dispose();
            _connectCancellationTokenSource = null;

            _redisClient?.Dispose();
            _redisClient = null;
        }

        [return: Pin(Name = "Output")]
        public RedisClient? Update(string? configuration = "localhost:6379", Action<ConfigurationOptions>? configure = null, int database = -1, SerializationFormat serializationFormat = SerializationFormat.MessagePack, bool connectAsync = true)
        {
            if (configuration != _configuration)
            {
                _redisClient?.Dispose();
                _redisClient = null;

                _configuration = configuration;

                if (_connectCancellationTokenSource != null && _connectTask != null)
                {
                    _connectCancellationTokenSource.Cancel();
                    _connectCancellationTokenSource.Dispose();
                }

                _connectCancellationTokenSource = new CancellationTokenSource();
                _connectTask = Reconnect(configuration, configure, connectAsync, _connectCancellationTokenSource.Token, _connectTask);
            }

            if (_redisClient != null)
            {
                _redisClient.Database = database;
                _redisClient.Format = serializationFormat;
            }

            return _redisClient;
        }

        public bool IsConnected => _redisClient != null && _redisClient.Multiplexer.IsConnected;

        public string ClientName => _redisClient?.ClientName ?? string.Empty;

        private async Task Reconnect(string? configuration, Action<ConfigurationOptions>? configure, bool connectAsync, CancellationToken cancellationToken, Task? existingConnectTask)
        {
            var options = new ConfigurationOptions();
            if (configuration != null)
                options = ConfigurationOptions.Parse(configuration);
            if (configure != null)
                options.Apply(configure);

            options.LoggerFactory ??= _nodeContext.AppHost.LoggerFactory;
            options.Protocol = RedisProtocol.Resp2;
            // Attach our unique id so we can identify our pub/sub connection later (see below)
            options.ClientName = $"{options.ClientName ?? options.Defaults.ClientName}{GetHashCode()}";
            // Needed to get the client list, see comment in RedisClient
            options.AllowAdmin = true;

            // Set initial retry count to 0, as we manage that ourselves in the reconnect loop (gives use more control)
            options.ConnectRetry = 0;
            // Don't queue commands while disconnected
            options.BacklogPolicy = BacklogPolicy.FailFast;

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
                        _redisClient = new RedisClient(_nodeContext.AppHost, multiplexer, _logger);
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
        }

        private void WriteIntoGlobalChannels(SubFrameMessage message)
        {
            _redisClient?.WriteIntoGlobalChannels(message);
        }

        private void SendData(SubFrameMessage message)
        {
            _redisClient?.SendData(message);
        }
    }
}
