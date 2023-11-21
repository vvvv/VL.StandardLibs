#nullable enable
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ServiceWire.NamedPipes;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Joins;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VL.Core;
using VL.Core.Import;
using VL.Core.Logging;
using VL.Core.Reactive;
using VL.Core.Utils;
using VL.Model;
using VL.Lang.PublicAPI;
using VL.Lib.Animation;
using VL.Lib.Collections;
using VL.Lib.Reactive;
using VL.Serialization.MessagePack;
using VL.Serialization.Raw;
using Stride.Core.Mathematics;
using System.Reflection;

[assembly: ImportAsIs(Namespace = "VL")]

namespace VL.IO.Redis
{
    [ProcessNode(FragmentSelection = FragmentSelection.Explicit)]
    public sealed class RedisModule : IModule, IDisposable
    {
        private readonly ILogger _logger;
        private readonly NodeContext _nodeContext;
        private readonly IChannel<ImmutableDictionary<string, BindingModel>> _modelStream;
        private readonly IDisposable _modelSubscription;
        private readonly IChannelHub _channelHub;
        private readonly RedisClientManager _redisClientManager;
        private RedisClient? _redisClient;

        [Fragment]
        public RedisModule(
            [Pin(Visibility = PinVisibility.Hidden)]   NodeContext nodeContext,
            [Pin(Visibility = PinVisibility.Hidden)]   AppHost appHost,
            [Pin(Visibility = PinVisibility.Hidden)]   IFrameClock frameClock,
            [Pin(Visibility = PinVisibility.Hidden)]   IChannelHub channelHub,
            [Pin(Visibility = PinVisibility.Optional)] IChannel<ImmutableDictionary<string, BindingModel>> model)
        {
            _nodeContext = nodeContext;
            _logger = nodeContext.GetLogger();

            if (model.IsValid())
                _modelStream = model;
            else
            {
                _modelStream = ChannelHelpers.CreateChannelOfType<ImmutableDictionary<string, BindingModel>>();
                _modelStream.Value = ImmutableDictionary<string, BindingModel>.Empty;
            }

            _channelHub = channelHub ?? appHost.Services.GetRequiredService<IChannelHub>();
            _redisClientManager = new RedisClientManager(nodeContext, frameClock);
            _channelHub.RegisterModule(this);

            _modelSubscription = _modelStream
                .Throttle(TimeSpan.FromSeconds(0.2))
                .ObserveOn(appHost.SynchronizationContext)
                .Subscribe(m =>
                {
                    if (m != null)
                        UpdateBindingsFromModel(m);

                    var solution = SessionNodes.CurrentSolution
                        .SetPinValue(nodeContext.Path.Stack, "Model", m);
                    solution.Confirm();
                });
        }

        public void Dispose() 
        {
            // TODO: UNREGISTER?
            //_channelHub.UNREGISTER(this);
            _modelSubscription?.Dispose();
            _redisClientManager.Dispose();
        }

        [Fragment]
        public void Update(string? configuration = "localhost:6379", Action<ConfigurationOptions>? configure = null, int database = -1, SerializationFormat serializationFormat = SerializationFormat.MessagePack)
        {
            var client = _redisClientManager.Update(configuration, configure, database, serializationFormat);
            if (client != _redisClient)
            {
                _redisClient = client;
                if (client != null)
                    UpdateBindingsFromModel(_modelStream.Value!);
            }
        }

        [Fragment]
        public RedisClient? Client => _redisClient;

        // Called from patched ModuleView
        public void AddBinding(string channelPath, IChannel channel, BindingModel bindingModel)
        {
            if (_redisClient is null)
                return;

            // Save in our pin - this will trigger a sync
            var model = _modelStream.Value!.SetItem(channelPath, bindingModel);
            _modelStream.Value = model;
        }

        private void UpdateBindingsFromModel(ImmutableDictionary<string, BindingModel> model)
        {
            if (_redisClient is null)
            {
                _logger.LogWarning("No active Redis client. Can't sync bindings.");
                return;
            }

            // Cleanup
            var obsoleteKeys = new List<string>();
            foreach (var entry in _redisClient._bindings)
                if (!model.TryGetValue(entry.Key, out var m) || m != entry.Value.model)
                    obsoleteKeys.Add(entry.Key);

            foreach (var key in obsoleteKeys)
            {
                var (_, binding) = _redisClient._bindings[key];
                binding.Dispose();
            }

            // Add new
            foreach (var (key, bindingModel) in model)
            {
                var channel = _channelHub.TryGetChannel(key);
                if (channel is null)
                {
                    _logger.LogWarning("Couldn't find global channel with name \"{channelName}\"", key);
                    continue;
                }

                _redisClient.AddBinding(bindingModel, channel, this);
            }
        }

        string IModule.Name => "Redis";

        string IModule.Description => "This module binds a channel to a Redis key";

        bool IModule.SupportsType(Type type) => true;
    }

    internal sealed class RedisModuleView : IModuleView
    {
        private readonly RedisModule _module;

        public RedisModuleView(RedisModule module)
        {
            _module = module;
        }

        public BindingUserEditingCapabilities? BindingEditingCapabilities =>
            BindingUserEditingCapabilities.OnlyAllowSingle | BindingUserEditingCapabilities.Editable | BindingUserEditingCapabilities.ManuallyRemovable;

        public IPlainProcessNode CreateAddBindingDialog(string channelPath, IChannel channel, IChannel<Action> responeChannel, IBinding? initialBinding, Vector2 expectedSize)
        {
            throw new NotImplementedException();
        }

        public void RemoveBinding(IBinding binding)
        {
            binding.Dispose();
        }
    }

    [ProcessNode(Name = "RedisClient")]
    public sealed class RedisClientManager : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();

        private readonly NodeContext _nodeContext;
        private readonly ILogger _logger;

        private string? _configuration;
        private RedisClient? _redisClient;

        public RedisClientManager(NodeContext nodeContext, IFrameClock frameClock)
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

    public class RedisClient : IDisposable
    {
        private readonly TransactionBuilder _transactionBuilder = new();
        private readonly ILogger _logger;
        internal readonly Dictionary<string, (BindingModel model, IDisposable binding)> _bindings = new();
        private readonly ConnectionMultiplexer _multiplexer;
        private readonly Subject<Unit> _networkSync = new Subject<Unit>();

        private ImmutableArray<IParticipant> _participants = ImmutableArray<IParticipant>.Empty;

        private Task? _lastTransaction;

        public RedisClient(ConnectionMultiplexer multiplexer, ILogger logger)
        {
            _multiplexer = multiplexer;
            _logger = logger;

            // HACK: It seems the StackExchange API is a little too high level here / doesn't support this yet properly:
            // 1) CLIENT TRACKING ON without the REDIRECT option requires RESP3, but StackExchange will crash in that case not being able to handle the incoming server message
            // 2) CLIENT TRACKING ON with the REDIRECT option only seems to work in RESP2, but in RESP2 we need to use a 2nd connection for Pub/Sub.
            //    However getting the ID of that 2nd connection is not possible in StackExchange (https://stackoverflow.com/questions/66964604/how-do-i-get-the-client-id-for-the-isubscriber-connection)
            //    we need to ask the server (requires the AllowAdmin option) for the client list and then identify our pub/sub connection.
            // Hopefully the situation should improve once https://github.com/StackExchange/StackExchange.Redis/tree/server-cache-invalidation is merged back.

            // This opens a Pub/Sub connection internally
            var subscriber = multiplexer.GetSubscriber();
            var _invalidations = subscriber.Subscribe(RedisChannel.Literal("__redis__:invalidate"));
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

        public void Dispose()
        {
            _multiplexer.Dispose();
        }

        [DefaultValue(SerializationFormat.MessagePack)]
        public SerializationFormat Format { private get; set; } = SerializationFormat.MessagePack;

        [DefaultValue(-1)]
        public int Database { internal get; set; } = -1;

        internal IObservable<Unit> NetworkSync => _networkSync;

        internal IDatabase GetDatabase() => _multiplexer.GetDatabase(Database);

        internal IServer? GetServer() => _multiplexer.GetServers().FirstOrDefault();

        internal ConnectionMultiplexer Multiplexer => _multiplexer;

        internal IDisposable Subscribe(IParticipant participant)
        {
            _participants = _participants.Add(participant);
            return Disposable.Create(() => _participants = _participants.Remove(participant));
        }

        internal ISubscriber GetSubscriber() => _multiplexer.GetSubscriber();

        internal IDisposable AddBinding(BindingModel model, IChannel channel, RedisModule? module = null)
        {
            if (_bindings.ContainsKey(model.Key))
                throw new InvalidOperationException($"The redis key \"{model.Key}\" is already bound to a different channel.");

            var binding = (IDisposable)Activator.CreateInstance(
                type: typeof(Binding<>).MakeGenericType(channel.ClrTypeOfValues),
                args: new object?[] { this, channel, model, module })!;
            _bindings[model.Key] = (model, binding);
            return Disposable.Create(() =>
            {
                _bindings.Remove(model.Key);
                binding.Dispose();
            });
        }

        internal RedisValue Serialize<T>(T? value, SerializationFormat? preferredFormat)
        {
            switch (GetEffectiveSerializationFormat(preferredFormat))
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

        internal T? Deserialize<T>(RedisValue redisValue, SerializationFormat? preferredFormat)
        {
            switch (GetEffectiveSerializationFormat(preferredFormat))
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

        SerializationFormat GetEffectiveSerializationFormat(SerializationFormat? preferredFormat) => preferredFormat ?? Format;

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

        internal void BeginFrame(FrameTimeMessage _)
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

            // Simulate new network sync event -> values are now written back to the channels
            _networkSync.OnNext(default);
        }

        internal void EndFrame(FrameFinishedMessage _) 
        {
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
            var database = _multiplexer.GetDatabase(Database);
            _lastTransaction = _transactionBuilder.BuildAndExecuteAsync(database);
        }
    }

    public record struct BindingModel(
        string Key, 
        Initialization Initialization = Initialization.Redis,
        RedisBindingType BindingType = RedisBindingType.SendAndReceive, 
        CollisionHandling CollisionHandling = default, 
        SerializationFormat? SerializationFormat = default);

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

    [ProcessNode(Name = "Binding")]
    public class BindingNode : IDisposable
    {
        private readonly SerialDisposable _current = new();
        private readonly NodeContext _nodeContext;

        private (RedisClient? client, IChannel? channel, string? key, Initialization initialization, RedisBindingType bindingType, CollisionHandling collisionHandling, SerializationFormat? serializationFormat) _config;

        public BindingNode(NodeContext nodeContext)
        {
            _nodeContext = nodeContext;
        }

        public void Update(
            RedisClient? client, 
            IChannel? channel, 
            string? key,
            Initialization initialization = Initialization.Redis,
            RedisBindingType bindingType = RedisBindingType.SendAndReceive,
            CollisionHandling collisionHandling = default,
            SerializationFormat? serializationFormat = default)
        {
            var config = (client, channel, key, initialization, bindingType, collisionHandling, serializationFormat);
            if (config == _config)
                return;

            _config = config;
            _current.Disposable = null;

            if (client is null || channel is null || string.IsNullOrWhiteSpace(key))
                return;

            var model = new BindingModel(key, initialization, bindingType, collisionHandling, serializationFormat);
            try
            {
                _current.Disposable = client.AddBinding(model, channel);
            }
            catch (Exception e)
            {
                // TODO: Use logger / add better API which also works in exported apps
                _current.Disposable = IVLRuntime.Current?.AddException(_nodeContext, e);
            }
        }

        public void Dispose()
        {
            _current.Dispose();
        }
    }

    internal class Binding<T> : IParticipant, IDisposable, IBinding
    {
        private readonly SerialDisposable _clientSubscription = new();
        private readonly SerialDisposable _channelSubscription = new();
        private readonly string _authorId;
        private readonly RedisClient _client;
        private readonly IChannel<T> _channel;
        private readonly BindingModel _bindingModel;
        private readonly RedisModule? _module;

        private bool _initialized;
        private bool _weHaveNewData;
        private bool _othersHaveNewData;

        public Binding(RedisClient client, IChannel<T> channel, BindingModel bindingModel, RedisModule? module)
        {
            _client = client;
            _channel = channel;
            _bindingModel = bindingModel;
            _module = module;

            _initialized = bindingModel.Initialization == Initialization.None;
            _authorId = this.GetHashCode().ToString();

            _clientSubscription.Disposable = client.Subscribe(this);
            _channelSubscription.Disposable = channel.Subscribe(v =>
            {
                if (_channel.LatestAuthor != _authorId)
                {
                    _weHaveNewData = true;
                }
            });

            _channel.AddComponent(this);
        }

        public void Dispose()
        {
            _channel.RemoveComponent(this);
            _clientSubscription.Dispose();
            _channelSubscription.Dispose();
        }

        void IParticipant.Invalidate(string key)
        {
            if (key == _bindingModel.Key)
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
                if (_bindingModel.CollisionHandling == CollisionHandling.LocalWins)
                    needToReadFromDb = false;
                else if (_bindingModel.CollisionHandling == CollisionHandling.RedisWins)
                    needToWriteToDb = false;
            }

            builder.Add(async transaction =>
            {
                _initialized = true;
                _weHaveNewData = false;
                _othersHaveNewData = false;

                var key = _bindingModel.Key;
                if (needToWriteToDb)
                {
                    var redisValue = _client.Serialize(_channel.Value, _bindingModel.SerializationFormat);
                    _ = transaction.StringSetAsync(key, redisValue, flags: CommandFlags.FireAndForget);
                }
                if (needToReadFromDb)
                {
                    var redisValue = await transaction.StringGetAsync(key).ConfigureAwait(false);
                    if (redisValue.HasValue)
                    {
                        var value = _client.Deserialize<T>(redisValue, _bindingModel.SerializationFormat);

                        await _client.NetworkSync.Take(1);

                        _channel.SetValueAndAuthor(value, author: _authorId);
                    }
                }
            });

            bool NeedToReadFromDb()
            {
                var bindingType = _bindingModel.BindingType;
                if (bindingType == RedisBindingType.AlwaysReceive)
                    return true;
                if (!bindingType.HasFlag(RedisBindingType.Receive))
                    return false;
                if (_initialized)
                    return _othersHaveNewData;
                return _bindingModel.Initialization == Initialization.Redis;
            }

            bool NeedToWriteToDb()
            {
                if (!_bindingModel.BindingType.HasFlag(RedisBindingType.Send))
                    return false;
                if (_initialized)
                    return _weHaveNewData;
                return _bindingModel.Initialization == Initialization.Local;
            }
        }

        IModule? IBinding.Module => _module;

        string IBinding.ShortLabel => "Redis";

        string? IBinding.Description => _bindingModel.Key;

        BindingType IBinding.BindingType
        {
            get
            {
                switch (_bindingModel.BindingType)
                {
                    case RedisBindingType.None:
                        return BindingType.None;
                    case RedisBindingType.Send:
                        return BindingType.Send;
                    case RedisBindingType.Receive:
                        return BindingType.Receive;
                    case RedisBindingType.SendAndReceive:
                        return BindingType.SendAndReceive;
                    case RedisBindingType.AlwaysReceive:
                        return BindingType.None;
                    default:
                        return BindingType.None;
                }
            }
        }
    }

    // TODO: Fix node name - has stupid `1 inside!
    [ProcessNode(Name = "Publish")]
    public class Publish<T> : IDisposable
    {
        private readonly SerialDisposable _subscription = new();
        private readonly ILogger _logger;

        private (RedisClient? client, string? redisChannel, IObservable<T>? value, RedisChannel.PatternMode pattern, SerializationFormat? format) _config;

        // TODO: For unit testing it would be nice to take the logger directly!
        public Publish(NodeContext nodeContext)
        {
            _logger = nodeContext.GetLogger();
        }

        public void Dispose() 
        {
            _subscription.Dispose();
        }

        public void Update(
            RedisClient? client, 
            string? redisChannel, 
            IObservable<T>? stream, 
            RedisChannel.PatternMode pattern = RedisChannel.PatternMode.Auto, 
            SerializationFormat? serializationFormat = default)
        {
            var config = (client, redisChannel, stream, pattern, serializationFormat);
            if (config == _config)
                return;

            _config = config;
            _subscription.Disposable = null;

            if (client is null || redisChannel is null || stream is null)
                return;

            _subscription.Disposable = stream.Subscribe(v =>
            {
                try
                {
                    var subscriber = client.GetSubscriber();
                    var channel = new RedisChannel(redisChannel, pattern);
                    var value = client.Serialize(v, serializationFormat);
                    subscriber.Publish(channel, value, CommandFlags.FireAndForget);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Exception while publishing.");
                }
            });
        }
    }

    [ProcessNode(Name = "Subscribe")]
    public class Subscribe<T> : IDisposable
    {
        private readonly SerialDisposable _subscription = new();
        private readonly Subject<T?> _subject = new();
        private readonly ILogger _logger;

        record struct Config(RedisClient? Client, string? Channel, RedisChannel.PatternMode Pattern, SerializationFormat? Format, bool ProcessMessagesConcurrently);

        private Config _config;

        // TODO: For unit testing it would be nice to take the logger directly!
        public Subscribe(NodeContext nodeContext)
        {
            _logger = nodeContext.GetLogger();
        }

        public void Dispose()
        {
            _subscription.Dispose();
        }

        public IObservable<T?> Update(
            RedisClient? client,
            string? redisChannel,
            RedisChannel.PatternMode pattern = RedisChannel.PatternMode.Auto,
            SerializationFormat? serializationFormat = default,
            bool processMessagesConcurrently = false)
        {
            var config = new Config(client, redisChannel, pattern, serializationFormat, processMessagesConcurrently);
            if (config != _config)
            {
                _config = config;
                Resubscribe(config);
            }
            return _subject;
        }

        private void Resubscribe(Config config)
        {
            // Unsubscribe
            _subscription.Disposable = null;

            var client = config.Client;
            if (client is null || config.Channel is null)
                return;

            var subscriber = client.GetSubscriber();
            var channel = new RedisChannel(config.Channel, config.Pattern);

            if (config.ProcessMessagesConcurrently)
            {
                Action<RedisChannel, RedisValue> handler = (redisChannel, redisValue) =>
                {
                    try
                    {
                        var value = client.Deserialize<T>(redisValue, config.Format);
                        _subject.OnNext(value);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Unexpected exception in subscribe.");
                    }
                };
                subscriber.Subscribe(channel, handler);
                _subscription.Disposable = Disposable.Create(() => subscriber.Unsubscribe(channel, handler));
            }
            else
            {
                Action<ChannelMessage> handler = (channelMessage) =>
                {
                    try
                    {
                        var redisValue = channelMessage.Message;
                        var value = client.Deserialize<T>(redisValue, config.Format);
                        _subject.OnNext(value);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Unexpected exception in subscribe.");
                    }
                };

                var queue = subscriber.Subscribe(channel);
                queue.OnMessage(handler);
                _subscription.Disposable = Disposable.Create(() => queue.Unsubscribe());
            }
        }
    }

    public static class ServerManagement
    {
        public static bool DeleteKey(this RedisClient? client, string? key, bool apply)
        {
            if (!apply)
                return false;

            if (client is null)
                return false;

            if (string.IsNullOrEmpty(key))
                return false;

            return client.GetDatabase().KeyDelete(key);
        }

        public static void FlushDB(this RedisClient? client, bool apply)
        {
            if (!apply)
                return;

            if (client is null)
                return;

            var server = client.GetServer();
            if (server is null)
                return;

            server.FlushDatabase(client.Database);
        }

        public static Spread<string> Scan(this RedisClient? client, string? pattern, bool apply)
        {
            if (!apply)
                return Spread<string>.Empty;

            if (client is null)
                return Spread<string>.Empty;

            var server = client.GetServer();
            if (server is null)
                return Spread<string>.Empty;

            var builder = new SpreadBuilder<string>();
            foreach (var key in server.Keys(client.Database, pattern))
                builder.Add(key.ToString());

            return builder.ToSpread();
        }
    }
}
