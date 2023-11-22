using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using VL.Core;
using VL.Core.Import;
using VL.Lib.Animation;
using VL.Model;

namespace VL.IO.Redis
{
    [ProcessNode(Name = "RedisClient")]
    public sealed class RedisClientManager : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();

        private readonly NodeContext _nodeContext;
        private readonly ILogger _logger;

        private string? _configuration;
        private RedisClient? _redisClient;

        public RedisClientManager(
            [Pin(Visibility = PinVisibility.Hidden)] NodeContext nodeContext,
            [Pin(Visibility = PinVisibility.Hidden)] IFrameClock frameClock)
        {
            _nodeContext = nodeContext;
            _logger = nodeContext.GetLogger();

            frameClock.GetTicks()
                .Subscribe(BeginFrame)
                .DisposeBy(_disposables);

            frameClock.GetFrameFinished()
                .Subscribe(EndFrame)
                .DisposeBy(_disposables);
        }

        public void Dispose()
        {
            _redisClient?.Dispose();
            _redisClient = null;
        }

        [return: Pin(Name = "Output")]
        public RedisClient? Update(string? configuration = "localhost:6379", Action<ConfigurationOptions>? configure = null, int database = -1, SerializationFormat serializationFormat = SerializationFormat.MessagePack)
        {
            if (configuration != _configuration)
            {
                _configuration = configuration;
                Reconnect(configuration, configure);
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

        private void Reconnect(string? configuration, Action<ConfigurationOptions>? configure)
        {
            _redisClient?.Dispose();
            _redisClient = null;

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

            var multiplexer = ConnectionMultiplexer.Connect(options);
            _redisClient = new RedisClient(multiplexer, _logger);
        }

        private void BeginFrame(FrameTimeMessage message)
        {
            _redisClient?.BeginFrame(message);
        }

        private void EndFrame(FrameFinishedMessage message)
        {
            _redisClient?.EndFrame(message);
        }
    }
}
