using MathNet.Numerics.Distributions;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using VL.Core;
using VL.Core.Import;
using VL.IO.Redis.Internal;
using VL.Lib.Animation;
using VL.Model;
using VL.Serialization.MessagePack;
using VL.Serialization.Raw;

namespace VL.IO.Redis
{
    /// <summary>
    /// Sets up a connection to a database on a Redis server
    /// </summary>
    internal sealed class RedisClientManager : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();

        private readonly NodeContext _nodeContext;
        private readonly ILogger _logger;

        private Task? _connectTask;
        private bool _disposed;

        private string? _configuration;
        private RedisClientInternal? _redisClient;

        private readonly TransactionBuilder _transactionBuilder = new();
        private ImmutableArray<IParticipant> _participants = ImmutableArray<IParticipant>.Empty;

        private Task? _lastTransaction;

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
        public RedisClientInternal? Update(string? configuration = "localhost:6379", Action<ConfigurationOptions>? configure = null, int database = -1, SerializationFormat serializationFormat = SerializationFormat.MessagePack, bool connectAsync = true)
        {
            if (configuration != _configuration)
            {
                _redisClient?.Dispose();
                _redisClient = null;

                // Do not start a new connection attempt as long as we're still in another one
                if (_connectTask is null || _connectTask.IsCompleted)
                {
                    _configuration = configuration;
                    _connectTask = Reconnect(configuration, configure, connectAsync);
                }
            }

            if (_redisClient != null)
            {
                _redisClient.Database = database;
            }

            return _redisClient;
        }

        public bool IsConnected => _redisClient != null && _redisClient.Multiplexer.IsConnected;

        public string ClientName => _redisClient?.ClientName ?? string.Empty;

        internal ImmutableArray<IParticipant> Participants => _participants;

        private async Task Reconnect(string? configuration, Action<ConfigurationOptions>? configure, bool connectAsync)
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

            try
            {
                var multiplexer = connectAsync
                        ? await ConnectionMultiplexer.ConnectAsync(options)
                        : ConnectionMultiplexer.Connect(options);

                if (!_disposed)
                    _redisClient = new RedisClientInternal(_nodeContext.AppHost, multiplexer, _logger, this);
                else
                    multiplexer.Dispose();
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Failed to connect");
            }
        }

        internal IDisposable Subscribe(IParticipant participant)
        {
            _participants = _participants.Add(participant);
            return Disposable.Create(() => _participants = _participants.Remove(participant));
        }

        private void WriteIntoGlobalChannels(SubFrameMessage message)
        {
            if (_redisClient is null)
                return;

            // Make room for the next transaction
            if (_lastTransaction != null)
            {
                if (_lastTransaction.IsCompleted)
                {
                    if (_lastTransaction.IsFaulted)
                    {
                        _logger?.LogError(_lastTransaction.Exception, "Exception in last transaction.");
                        _lastTransaction = null;
                    }

                    _lastTransaction = null;
                }
            }

            // Simulate new network sync event -> values are now written back to the channels
            _redisClient.NetworkSync.OnNext(default);
        }

        private void SendData(SubFrameMessage message)
        {
            if (_redisClient is null)
                return;

            // Do not build a new transaction while another one is still in progress
            if (_lastTransaction != null)
                return;

            // 1) Collect changes and if necessary build a new transaction
            _transactionBuilder.Clear();
            foreach (var participant in _participants)
                participant.BuildUp(_redisClient, _transactionBuilder);

            if (_transactionBuilder.IsEmpty)
                return;

            // 2) Send the transaction
            var database = _redisClient.Multiplexer.GetDatabase(_redisClient.Database);
            _lastTransaction = _transactionBuilder.BuildAndExecuteAsync(database);
        }
    }
}
