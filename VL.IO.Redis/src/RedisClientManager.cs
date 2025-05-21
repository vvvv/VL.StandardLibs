using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using VL.Core;
using VL.Core.Import;
using VL.IO.Redis.Experimental;
using VL.Lib.Animation;
using VL.Model;

namespace VL.IO.Redis
{
    /// <summary>
    /// Sets up a connection to a database on a Redis server
    /// </summary>
    //[ProcessNode(Name = "RedisClient")]
    public sealed class RedisClientManager : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();

        private readonly NodeContext _nodeContext;
        private readonly ILogger _logger;

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

            _redisClient?.Dispose();
            _redisClient = null;
        }

        [return: Pin(Name = "Output")]
        public RedisClient? Update(string? configuration = "localhost:6379", Action<ConfigurationOptions>? configure = null, int database = -1, 
            SerializationFormat serializationFormat = SerializationFormat.MessagePack, bool connectAsync = true, RedisModule module = default)
        {
            if (configuration != _configuration)
            {
                _redisClient?.Dispose();
                _redisClient = null;

                // Do not start a new connection attempt as long as we're still in another one
                if (_connectTask is null || _connectTask.IsCompleted)
                {
                    _configuration = configuration;
                    _connectTask = Reconnect(configuration, configure, connectAsync, module);
                }
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

        public ConfigurationOptions Options { get; set; }

        private async Task Reconnect(string? configuration, Action<ConfigurationOptions>? configure, bool connectAsync, RedisModule module)
        {
            var options = new ConfigurationOptions();
            if (configuration != null)
                options = ConfigurationOptions.Parse(configuration);
            if (configure != null)
                options.Apply(configure);

            ref var abortOnConnectFail = ref GetAbortOnConnectFail(options);
            if (!abortOnConnectFail.HasValue)
                options.AbortOnConnectFail = false;
            ref var connectRetry = ref GetConnectRetry(options);
            if (!connectRetry.HasValue)
                options.ConnectRetry = int.MaxValue;

            options.LoggerFactory ??= _nodeContext.AppHost.LoggerFactory;
            options.Protocol = RedisProtocol.Resp2;
            // Attach our unique id so we can identify our pub/sub connection later (see below)
            options.ClientName = $"{options.ClientName ?? options.Defaults.ClientName}{GetHashCode()}";
            // Needed to get the client list, see comment in RedisClient
            options.AllowAdmin = true;

            try
            {
                var multiplexer = connectAsync
                        ? await ConnectionMultiplexer.ConnectAsync(options)
                        : ConnectionMultiplexer.Connect(options);

                if (!_disposed)
                    _redisClient = new RedisClient(_nodeContext.AppHost, multiplexer, _logger, module);
                else
                    multiplexer.Dispose();
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Failed to connect");
            }

            Options = options;

            [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "abortOnConnectFail")]
            extern static ref bool? GetAbortOnConnectFail(ConfigurationOptions c);

            [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "connectRetry")]
            extern static ref int? GetConnectRetry(ConfigurationOptions c);
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
