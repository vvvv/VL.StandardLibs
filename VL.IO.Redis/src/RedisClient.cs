#nullable enable
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VL.Core;
using VL.Core.Import;
using VL.Core.Logging;
using VL.Lib.Animation;
using VL.Lib.Reactive;
using VL.Serialization.MessagePack;
using VL.Serialization.Raw;

[assembly: ImportAsIs(Namespace = "VL")]

namespace VL.IO.Redis
{
    [ProcessNode(HasStateOutput = true)]
    public class RedisClient : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        private readonly TransactionBuilder _transactionBuilder = new();
        private readonly IFrameClock _frameClock;
        private readonly ILoggerFactory? _loggerFactory;
        private readonly ILogger _logger;

        private ImmutableArray<IParticipant> _participants = ImmutableArray<IParticipant>.Empty;

        private ConnectionMultiplexer? _multiplexer;

        private ChannelMessageQueue? _invalidations;
        private ConfigurationOptions? _configuration;
        private Task? _lastTransaction;

        public RedisClient(IFrameClock frameClock, ILoggerFactory? loggerFactory)
        {
            _frameClock = frameClock;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory?.CreateLogger<RedisClient>() ?? NullLogger<RedisClient>.Instance;

            frameClock.GetTicks()
                .Subscribe(BeginFrame)
                .DisposeBy(_disposables);

            frameClock.GetFrameFinished()
                .Subscribe(EndFrame)
                .DisposeBy(_disposables);
        }

        public void Dispose()
        {
            _disposables.Dispose();
            Disconnect();
        }

        public ConfigurationOptions? Options
        {
            get => _configuration;
            set
            {
                if (_configuration != value)
                {
                    _configuration = value;
                    Reconnect(value?.Clone());
                }
            }
        }

        public SerializationFormat Format { internal get; set; }

        internal IFrameClock FrameClock => _frameClock;

        internal IDisposable Subscribe(IParticipant participant)
        {
            _participants = _participants.Add(participant);
            return Disposable.Create(() => _participants = _participants.Remove(participant));
        }

        private void Connect(ConfigurationOptions options)
        {
            try
            {
                options.LoggerFactory ??= _loggerFactory;
                options.Protocol = RedisProtocol.Resp2;
                // Attach our unique id so we can identify our pub/sub connection later (see below)
                options.ClientName = $"{options.ClientName ?? options.Defaults.ClientName}{GetHashCode()}";
                // Needed to get the client list, see bwloe
                options.AllowAdmin = true;

                _multiplexer = ConnectionMultiplexer.Connect(options);

                // HACK: It seems the StackExchange API is a little too high level here / doesn't support this yet properly:
                // 1) CLIENT TRACKING ON without the REDIRECT option requires RESP3, but StackExchange will crash in that case not being able to handle the incoming server message
                // 2) CLIENT TRACKING ON with the REDIRECT option only seems to work in RESP2, but in RESP2 we need to use a 2nd connection for Pub/Sub.
                //    However getting the ID of that 2nd connection is not possible in StackExchange (https://stackoverflow.com/questions/66964604/how-do-i-get-the-client-id-for-the-isubscriber-connection)
                //    we need to ask the server (requires the AllowAdmin option) for the client list and then identify our pub/sub connection.
                // Hopefully the situation should improve once https://github.com/StackExchange/StackExchange.Redis/tree/server-cache-invalidation is merged back.

                // This opens a Pub/Sub connection internally
                var subscriber = _multiplexer.GetSubscriber();
                _invalidations = subscriber.Subscribe(RedisChannel.Literal("__redis__:invalidate"));
                _invalidations.OnMessage(OnInvalidationMessage);

                // Let's try to find that one now
                foreach (var s in _multiplexer.GetServers())
                {
                    var pubSubClient = s.ClientList().FirstOrDefault(c => c.Name == _multiplexer.ClientName && c.ClientType == ClientType.PubSub);
                    if (pubSubClient != null)
                    {
                        s.Execute("CLIENT", new object[] { "TRACKING", "ON", "REDIRECT", pubSubClient.Id.ToString(), "BCAST", "NOLOOP" });
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Exception while connecting.");
            }
        }

        private void Disconnect()
        {
            try
            {
                if (_invalidations != null)
                    _invalidations.Unsubscribe();

                if (_multiplexer != null)
                    _multiplexer.Dispose();
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Exception while disconnecting.");
            }
            finally
            {
                _invalidations = null;
                _multiplexer = null;
            }
        }

        private void Reconnect(ConfigurationOptions? options)
        {
            Disconnect();

            if (options != null)
                Connect(options);
        }

        private void OnInvalidationMessage(ChannelMessage message)
        {
            var key = message.Message.ToString();
            if (key is null)
                return;

            if (_logger.IsEnabled(LogLevel.Trace))
                _logger.LogTrace("Redis invalidated {key}", key);

            foreach (var p in _participants)
                p.Invalidate(key);
        }

        private void BeginFrame(FrameTimeMessage _)
        {
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
        }

        private void EndFrame(FrameFinishedMessage _) 
        {
            if (_multiplexer is null)
                return;

            // Do not build a new transaction while another one is still in progress
            if (_lastTransaction != null)
                return;

            // 1) Collect changes and if necessary build a new transaction
            _transactionBuilder.Clear();
            foreach (var participant in _participants)
                participant.BuildUp(_transactionBuilder);

            if (_transactionBuilder.IsEmpty)
                return;

            // 2) Send the transaction
            var database = _multiplexer.GetDatabase();
            _lastTransaction = _transactionBuilder.BuildAndExecuteAsync(database);
        }
    }

    sealed class TransactionBuilder
    {
        private readonly List<Func<ITransaction, Task>> _actions = new();
        private readonly List<Task> _tasks = new();

        public bool IsEmpty => _actions.Count == 0;

        public CommandFlags CommandFlags { get; set; }

        public void Add(Func<ITransaction, Task> asyncAction)
        {
            _actions.Add(asyncAction);
        }

        public Task BuildAndExecuteAsync(IDatabase database)
        {
            _tasks.Clear();

            var transaction = database.CreateTransaction();
            foreach (var action in _actions)
                _tasks.Add(action(transaction));
            _tasks.Add(transaction.ExecuteAsync(CommandFlags));

            return Task.WhenAll(_tasks);
        }

        public void Clear()
        {
            _actions.Clear();
            CommandFlags = CommandFlags.None;
        }
    }

    interface IParticipant
    {
        void BuildUp(TransactionBuilder builder);
        void Invalidate(string key);
    }

    [ProcessNode]
    public class Binding : IDisposable
    {
        private readonly SerialDisposable _current = new();
        private (RedisClient? client, IChannel? channel, string? key, Initialization initialization, RedisBindingType bindingType, CollisionHandling collisionHandling, SerializationFormat serializationFormat) _config;

        public void Update(RedisClient? client, IChannel? channel, string? key, Initialization initialization, RedisBindingType bindingType, CollisionHandling collisionHandling, SerializationFormat serializationFormat)
        {
            var config = (client, channel, key, initialization, bindingType, collisionHandling, serializationFormat);
            if (config == _config)
                return;

            _config = config;
            _current.Disposable = null;

            if (client is null || channel is null || key is null)
                return;

            _current.Disposable = Activator.CreateInstance(
                type: typeof(Binding<>).MakeGenericType(channel.ClrTypeOfValues),
                args: new object[] { client, channel, key, initialization, bindingType, collisionHandling, serializationFormat }) as IDisposable;
        }

        public void Dispose()
        {
            _current.Dispose();
        }
    }

    internal class Binding<T> : IParticipant, IDisposable
    {
        private readonly SerialDisposable _clientSubscription = new();
        private readonly SerialDisposable _channelSubscription = new();
        private readonly string _authorId;
        private readonly RedisClient _client;
        private readonly IChannel<T> _channel;
        private readonly string _key;
        private readonly Initialization _initialization;
        private readonly RedisBindingType _bindingType;
        private readonly CollisionHandling _collisionHandling;
        private readonly SerializationFormat? _format;

        private bool _initialized;
        private bool _weHaveNewData;
        private bool _othersHaveNewData;

        public Binding(RedisClient client, IChannel<T> channel, string key, Initialization initialization, RedisBindingType bindingType, CollisionHandling collisionHandling, SerializationFormat serializationFormat)
        {
            _client = client;
            _channel = channel;
            _key = key;
            _initialization = initialization;
            _bindingType = bindingType;
            _collisionHandling = collisionHandling;
            _format = serializationFormat;

            _initialized = initialization == Initialization.None;
            _authorId = this.GetHashCode().ToString();

            _clientSubscription.Disposable = client.Subscribe(this);
            _channelSubscription.Disposable = channel.Subscribe(v =>
            {
                if (_channel.LatestAuthor != _authorId)
                {
                    _weHaveNewData = true;
                }
            });
        }

        public void Dispose()
        {
            _clientSubscription.Dispose();
            _channelSubscription.Dispose();
        }

        void IParticipant.Invalidate(string key)
        {
            if (key == _key)
                _othersHaveNewData = true;
        }

        void IParticipant.BuildUp(TransactionBuilder builder)
        {
            var needToReadFromDb = NeedToReadFromDb();
            var needToWriteToDb = NeedToWriteToDb();
            if (!needToReadFromDb && !needToWriteToDb)
                return;

            if (needToReadFromDb && needToWriteToDb)
            {
                if (_collisionHandling == CollisionHandling.LocalWins)
                    needToReadFromDb = false;
                else if (_collisionHandling == CollisionHandling.RedisWins)
                    needToWriteToDb = false;
            }

            builder.Add(async transaction =>
            {
                _initialized = true;
                _weHaveNewData = false;
                _othersHaveNewData = false;

                if (needToWriteToDb)
                {   
                    _ = transaction.StringSetAsync(_key, Serialize(_channel.Value), flags: CommandFlags.FireAndForget);
                }
                if (needToReadFromDb)
                {
                    var redisValue = await transaction.StringGetAsync(_key).ConfigureAwait(false);
                    if (redisValue.HasValue)
                    {
                        var value = Deserialize(redisValue);

                        var networkSync = _client.FrameClock.GetTicks();
                        await networkSync.Take(1);

                        _channel.SetValueAndAuthor(value, author: _authorId);
                    }
                }
            });

            bool NeedToReadFromDb()
            {
                if (_bindingType == RedisBindingType.AlwaysReceive)
                    return true;
                if (!_bindingType.HasFlag(RedisBindingType.Receive))
                    return false;
                if (_initialized)
                    return _othersHaveNewData;
                return _initialization == Initialization.Redis;
            }

            bool NeedToWriteToDb()
            {
                if (!_bindingType.HasFlag(RedisBindingType.Send))
                    return false;
                if (_initialized)
                    return _weHaveNewData;
                return _initialization == Initialization.Local;
            }

            RedisValue Serialize(T? value)
            {
                switch (GetEffectiveSerializationFormat())
                {
                    case SerializationFormat.Raw:
                        return RawSerialization.Serialize(value);
                    case SerializationFormat.MessagePack:
                        return MessagePackSerialization.Serialize(value);
                    case SerializationFormat.Json:
                        return MessagePackSerialization.SerializeJson(value);
                    default:
                        throw new NotImplementedException();
                }
            }

            T? Deserialize(RedisValue redisValue)
            {
                switch (GetEffectiveSerializationFormat())
                {
                    case SerializationFormat.Raw:
                        return RawSerialization.Deserialize<T>(redisValue);
                    case SerializationFormat.MessagePack:
                        return MessagePackSerialization.Deserialize<T>(redisValue);
                    case SerializationFormat.Json:
                        return MessagePackSerialization.DeserializeJson<T>(redisValue.ToString());
                    default:
                        throw new NotImplementedException();
                }
            }

            SerializationFormat GetEffectiveSerializationFormat() => _format ?? _client.Format;
        }
    }
}
