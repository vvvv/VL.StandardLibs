using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using VL.Core;
using VL.Core.Import;
using VL.Core.Reactive;
using VL.IO.Redis.Internal;
using VL.Lang.PublicAPI;
using VL.Lib.Animation;
using VL.Lib.Collections;
using VL.Lib.Reactive;
using VL.Model;
using VL.Serialization.MessagePack;
using VL.Serialization.Raw;

namespace VL.IO.Redis
{
    // Patch calls AddBinding and later RemoveBinding.
    // Both methods will subsequently push a new model.
    // Internally we only need to keep the model in sync with what bindings we have at runtime.
    [ProcessNode(FragmentSelection = FragmentSelection.Explicit)]
    public sealed class RedisClient : IModule, IDisposable
    {
        private readonly ILogger _logger;
        private readonly NodeContext _nodeContext;
        private readonly AppHost _appHost;
        private readonly IChannel<ImmutableDictionary<string, BindingModel>> _modelStream;
        private readonly IDisposable _modelSubscription;
        private readonly CompositeDisposable _disposables = new();
        private readonly IChannelHub _channelHub;
        private readonly RedisConnectionManager _redisConnectionManager;
        private readonly Subject<Unit> _networkSync = new Subject<Unit>();
        private readonly TransactionBuilder _transactionBuilder = new();
        private readonly ConcurrentDictionary<string, IRedisBinding> _bindings = new();
        private RedisConnection? _redisConnection;
        private string _nickname = string.Empty;
        private string? _lastConfiguration;
        private Optional<string> _lastNickname;
        private readonly IChannel<Unit> _onModuleModelChanged = ChannelHelpers.CreateChannelOfType<Unit>();
        private Task? _lastTransaction;

        [Fragment]
        public RedisClient(
            [Pin(Visibility = PinVisibility.Hidden)]   NodeContext nodeContext,
            [Pin(Visibility = PinVisibility.Optional)] IChannel<ImmutableDictionary<string, BindingModel>> model,
            [Pin(Visibility = PinVisibility.Optional)] bool showBindingColumn = true
            )
        {
            _nodeContext = nodeContext;
            _appHost = nodeContext.AppHost;
            _logger = nodeContext.GetLogger();

            if (model.IsValid())
                _modelStream = model;
            else
            {
                _modelStream = ChannelHelpers.CreateChannelOfType<ImmutableDictionary<string, BindingModel>>();
                _modelStream.Value = ImmutableDictionary<string, BindingModel>.Empty;
            }

            _channelHub = _appHost.Services.GetRequiredService<IChannelHub>();
            _redisConnectionManager = new RedisConnectionManager(nodeContext);

            if (showBindingColumn)
                _channelHub.RegisterModule(this);

            _modelSubscription = _modelStream
                .ObserveOn(_appHost.SynchronizationContext)
                .Merge(_channelHub.OnChannelsChanged)
                .Merge((IObservable<object>)_onModuleModelChanged)
                .Subscribe(_ =>
                {
                    var m = _modelStream.Value;
                    UpdateBindingsFromModel(m ?? ImmutableDictionary<string, BindingModel>.Empty);

                    var solution = IDevSession.Current?.CurrentSolution
                        .SetPinValue(nodeContext.Path.Stack, "Model", m);
                    solution?.Confirm();
                });

            var frameClock = _appHost.Services.GetRequiredService<IFrameClock>();
            frameClock.GetSubFrameEvent(SubFrameEvents.ModulesWriteGlobalChannels)
                .Subscribe(WriteIntoGlobalChannels)
                .DisposeBy(_disposables);

            frameClock.GetSubFrameEvent(SubFrameEvents.ModulesSendingData)
                .Subscribe(SendData)
                .DisposeBy(_disposables);

            UpdateBindingsFromModel(_modelStream.Value ?? ImmutableDictionary<string, BindingModel>.Empty);
        }

        public void Dispose() 
        {
            _channelHub.UnregisterModule(this);

            _disposables.Dispose();
            _modelSubscription?.Dispose();
            _redisConnectionManager.Dispose();

            foreach (var (_, binding) in _bindings)
                binding.Dispose();
            _bindings.Clear();
        }

        [Fragment]
        public void Update(
            string? configuration = "localhost:6379", 
            Optional<string> nickname = default,
            Action<ConfigurationOptions>? configure = null, 
            int database = -1,
            Initialization initialization = Initialization.Redis,
            BindingDirection bindingType = BindingDirection.InOut,
            [Pin(Visibility = PinVisibility.Optional)] CollisionHandling collisionHandling = CollisionHandling.None,
            SerializationFormat serializationFormat = SerializationFormat.MessagePack,
            [Pin(Visibility = PinVisibility.Optional)] Optional<TimeSpan> expiry = default,
            [Pin(Visibility = PinVisibility.Optional)] When when = When.Always,
            bool connectAsync = true)
        {
            Format = serializationFormat;
            Database = database;

            if (configuration != _lastConfiguration || nickname != _lastNickname)
            {
                _lastConfiguration = configuration;
                _lastNickname = nickname;
                _nickname = nickname.TryGetValue(string.IsNullOrEmpty(configuration) ? "" : configuration.Substring(0, configuration.IndexOf(':')));
            }

            var newmodel = Model with
            { 
                Initialization = initialization,
                BindingType = bindingType,
                CollisionHandling = collisionHandling,
                SerializationFormat = serializationFormat,
                Expiry = expiry.ToNullable(),
                When = when
            };

            var changed = Model != newmodel;
            Model = newmodel;
            if (changed)
                _onModuleModelChanged.SetValueAndAuthor(Unit.Default, null);

            var connection = _redisConnectionManager.Update(configuration, configure, connectAsync, this);
            if (connection != _redisConnection)
            {
                _redisConnection = connection;

                foreach (var (_, binding) in _bindings)
                    binding.Reset();

                UpdateBindingsFromModel(_modelStream.Value ?? ImmutableDictionary<string, BindingModel>.Empty);
            }
        }


        public ResolvedBindingModel Model { get; private set; } = new ResolvedBindingModel(Model: default, Key: string.Empty, PublicChannelPath: default);


        [Fragment]
        public RedisClient? Client => this;

        [Fragment]
        public bool IsConnected => _redisConnectionManager.IsConnected;

        [Fragment]
        public string ClientName => _redisConnectionManager.ClientName;

        [DefaultValue(-1)]
        public int Database { internal get; set; } = -1;

        [DefaultValue(SerializationFormat.MessagePack)]
        public SerializationFormat Format { private get; set; } = SerializationFormat.MessagePack;

        internal RedisConnection? CurrentConnection => _redisConnection;
        internal IObservable<RedisConnection?> ConnectionObservable => _redisConnectionManager.ConnectionObservable;

        internal IDatabase? GetDatabase() => _redisConnection?.GetDatabase(Database);

        internal IServer? GetServer() => _redisConnection?.GetServer();

        // Called from patched ModuleView
        internal void AddPersistentBinding(IChannel channel, BindingModel bindingModel)
        {
            // Save in our pin - this will trigger a sync
            var model = _modelStream.Value ?? ImmutableDictionary<string, BindingModel>.Empty;
            _modelStream.Value = model.SetItem(channel.Path!, bindingModel);
        }

        // Called from patched ModuleView
        internal void RemovePersistentBinding(IBinding binding)
        {
            var models = _modelStream.Value ?? ImmutableDictionary<string, BindingModel>.Empty;
            var resolvedBindingModel = binding.GetResolvedModel<ResolvedBindingModel>();
            if (resolvedBindingModel.PublicChannelPath != null)
            {
                var updatedModel = models.Remove(resolvedBindingModel.PublicChannelPath);
                if (updatedModel != models)
                    _modelStream.Value = updatedModel;
            }
        }

        ResolvedBindingModel _latestUpdateModel;
        ImmutableDictionary<string, ResolvedBindingModel> _latestResolvedModels = ImmutableDictionary<string, ResolvedBindingModel>.Empty;

        private void UpdateBindingsFromModel(ImmutableDictionary<string, BindingModel> model)
        {
            var allBindingsPotentiallyChanged = _latestUpdateModel != Model;
            _latestUpdateModel = Model;

            // Cleanup
            var obsoleteBindings = new List<IBinding>();
            foreach (var (_, binding) in _bindings)
            {
                if (binding.Module != this || binding.GotCreatedViaNode)
                    continue;

                var resolvedBindingModel = binding.GetResolvedModel<ResolvedBindingModel>();
                var key = resolvedBindingModel.Key;
                if (model.TryGetValue(key, out var bindingModel))
                {
                    // Did the model change?
                    if (allBindingsPotentiallyChanged || resolvedBindingModel.Model != bindingModel)
                        obsoleteBindings.Add(binding);
                }
                else
                {
                    // Deleted
                    obsoleteBindings.Add(binding);
                }
            }

            foreach (var binding in obsoleteBindings)
                binding.Dispose();

            // Add new
            foreach (var (channelName, bindingModel) in model)
            {
                var channel = _channelHub.TryGetChannel(channelName);
                if (channel is null)
                {
                    _logger.LogWarning("Couldn't find global channel with name \"{channelName}\"", channelName);
                    continue;
                }

                var key = bindingModel.ResolveKey(channelName);
                if (TryGetBinding(key, out var existingBinding))
                {
                    if (existingBinding.Module is null || existingBinding.Module != this)
                    {
                        _logger.LogError("Failed to add Redis binding for \"{channelName}\" because it is already bound", channelName);
                    }
                    continue;
                }

                try
                {
                    AddBinding(bindingModel.Resolve(this, channel), channel);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to add Redis binding for \"{channelName}\"", channelName);
                }
            }
        }

        internal RedisValue Serialize<T>(T? value, SerializationFormat? preferredFormat)
        {
            using var _ = _appHost.MakeCurrent();
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
            using var _ = _appHost.MakeCurrent();
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

        string IModule.Name => "Redis";

        string IModule.Description => "This module binds a channel to a Redis key";

        bool IModule.SupportsType(Type type) => true;

        string IModule.ConfigHint
        {
            get
            {
                if (!IsConnected)
                    return "Not connected";

                return _redisConnectionManager.Options?.ToString() ?? string.Empty;
            }
        }

        string IModule.Nickname => _nickname;

        NodeContext IModule.NodeContext => _nodeContext;

        float IModule.InterfaceVersion => 2;

        internal bool TryGetBinding(string key, [NotNullWhen(true)] out IRedisBinding? binding) => _bindings.TryGetValue(key, out binding);

        internal IRedisBinding AddBinding(ResolvedBindingModel model, IChannel channel, ILogger? logger = null)
        {
            if (_bindings.ContainsKey(model.Key))
                throw new InvalidOperationException($"The Redis key \"{model.Key}\" is already bound to a different channel.");

            var binding = (IRedisBinding)Activator.CreateInstance(
                type: typeof(Binding<>).MakeGenericType(channel.ClrTypeOfValues),
                args: [this, channel, model, logger])!;
            _bindings[model.Key] = binding;
            return binding;
        }

        internal void RemoveBinding(string key)
        {
            _bindings.Remove(key, out _);
        }

        internal void OnInvalidationMessage(ChannelMessage message)
        {
            var key = message.Message.ToString();
            if (key is null)
                return;

            if (_logger.IsEnabled(LogLevel.Trace))
                _logger.LogTrace("Redis invalidated {key}", key);

            if (_bindings.TryGetValue(key, out var binding))
                binding.Invalidate();
        }

        internal IObservable<Unit> NetworkSync => _networkSync;

        internal void WriteIntoGlobalChannels(SubFrameMessage _)
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

        internal void SendData(SubFrameMessage _)
        {
            var connection = _redisConnection;
            if (connection is null)
                return;

            // Do not build a new transaction while another one is still in progress
            if (_lastTransaction != null)
                return;

            // 1) Collect changes and if necessary build a new transaction
            _transactionBuilder.Clear();
            foreach (var (_, binding) in _bindings)
                binding.BuildUp(_transactionBuilder);

            if (_transactionBuilder.IsEmpty)
                return;

            // 2) Send the transaction
            var database = connection.GetDatabase(Database);
            _lastTransaction = _transactionBuilder.BuildAndExecuteAsync(database);
        }
    }
}
