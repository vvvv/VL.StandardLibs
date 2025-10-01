using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reactive.Linq;
using VL.Core;
using VL.Core.Import;
using VL.Core.Reactive;
using VL.Model;
using VL.Lang.PublicAPI;
using VL.Lib.Animation;
using VL.Lib.Collections;
using VL.Lib.Reactive;
using VL.IO.Redis.Internal;
using Stride.Core.Extensions;
using System.Reactive;

namespace VL.IO.Redis.Experimental
{
    // Patch calls AddBinding and later RemoveBinding.
    // Both methods will subsequently push a new model.
    // Internally we only need to keep the model in sync with what bindings we have at runtime.
    [ProcessNode(Name = "RedisClient", FragmentSelection = FragmentSelection.Explicit)]
    public sealed class RedisModule : IModule, IDisposable
    {
        private readonly ILogger _logger;
        private readonly NodeContext _nodeContext;
        private readonly IChannel<ImmutableDictionary<string, BindingModel>> _modelStream;
        private readonly IDisposable _modelSubscription;
        private readonly IChannelHub _channelHub;
        private readonly RedisClientManager _redisClientManager;
        private RedisClient? _redisClient;
        private string _nickname = string.Empty;
        private IChannel<Unit> _onModuleModelChanged = ChannelHelpers.CreateChannelOfType<Unit>();

        [Fragment]
        public RedisModule(
            [Pin(Visibility = PinVisibility.Hidden)]   NodeContext nodeContext,
            [Pin(Visibility = PinVisibility.Hidden)]   AppHost appHost,
            [Pin(Visibility = PinVisibility.Hidden)]   IFrameClock frameClock,
            [Pin(Visibility = PinVisibility.Hidden)]   IChannelHub channelHub,
            [Pin(Visibility = PinVisibility.Optional)] IChannel<ImmutableDictionary<string, BindingModel>> model,
            [Pin(Visibility = PinVisibility.Optional)] bool showBindingColumn = true
            )
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

            if (showBindingColumn)
                _channelHub.RegisterModule(this);

            _modelSubscription = _modelStream
                .ObserveOn(appHost.SynchronizationContext)
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
        }

        public void Dispose() 
        {
            // TODO: UNREGISTER?
            //_channelHub.UNREGISTER(this);
            _modelSubscription?.Dispose();
            _redisClientManager.Dispose();

            _channelHub.UnregisterModule(this);
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
            _nickname = nickname.TryGetValue(string.IsNullOrEmpty(configuration) ? "" : configuration.Substring(0, configuration.IndexOf(':')));

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

            var client = _redisClientManager.Update(configuration, configure, database, serializationFormat, connectAsync, this);
            if (client != _redisClient)
            {
                _redisClient = client;
                if (client != null)
                    UpdateBindingsFromModel(_modelStream.Value ?? ImmutableDictionary<string, BindingModel>.Empty);
            }
        }


        public ResolvedBindingModel Model = new ResolvedBindingModel(Model: default, Key: default, PublicChannelPath: default);


        [Fragment]
        public RedisClient? Client => _redisClient;

        [Fragment]
        public bool IsConnected => _redisClientManager.IsConnected;

        [Fragment]
        public string ClientName => _redisClientManager.ClientName;

        // Called from patched ModuleView
        public void AddBinding(IChannel channel, BindingModel bindingModel)
        {
            // Save in our pin - this will trigger a sync
            var model = _modelStream.Value ?? ImmutableDictionary<string, BindingModel>.Empty;
            _modelStream.Value = model.SetItem(channel.Path!, bindingModel);
        }

        // Called from patched ModuleView
        public void RemoveBinding(IBinding binding)
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
            if (_redisClient is null)
            {
                _logger.LogWarning("No active Redis client. Can't sync bindings.");
                return;
            }

            var allBindingsPotentiallyChanged = _latestUpdateModel != Model;
            _latestUpdateModel = Model;

            var newResolvedModels = ImmutableDictionary<string, ResolvedBindingModel>.Empty.ToBuilder();
            foreach (var binding in _redisClient.Bindings)
            {
                if (binding.Module != this || binding.GotCreatedViaNode)
                    continue;

                var resolvedBindingModel = binding.GetResolvedModel<ResolvedBindingModel>();
                newResolvedModels.Add(resolvedBindingModel.Key, resolvedBindingModel);
            }

            // Cleanup
            var obsoleteBindings = new List<IBinding>();
            foreach (var binding in _redisClient.Bindings)
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
                if (_redisClient.TryGetBinding(key, out var existingBinding))
                {
                    if (existingBinding.Module is null || existingBinding.Module != this)
                    {
                        _logger.LogError("Failed to add Redis binding for \"{channelName}\" because it is already bound", channelName);
                    }
                    continue;
                }

                try
                {
                    _redisClient.AddBinding(bindingModel.Resolve(_redisClient._module, channel), channel, this);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to add Redis binding for \"{channelName}\"", channelName);
                }
            }
        }

        string IModule.Name => "Redis";

        string IModule.Description => "This module binds a channel to a Redis key";

        bool IModule.SupportsType(Type type) => true;

        string IModule.ConfigHint => _redisClientManager.Options?.ToString() ?? string.Empty;

        string IModule.Nickname => _nickname;

        NodeContext IModule.NodeContext => _nodeContext;

        float IModule.InterfaceVersion => 2;
    }
}
