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

namespace VL.IO.Redis.Experimental
{
    // Patch calls AddBinding and later RemoveBinding.
    // Both methods will subsequently push a new model.
    // Internally we only need to keep the model in sync with what bindings we have at runtime.
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
                .ObserveOn(appHost.SynchronizationContext)
                .Subscribe(m =>
                {
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
        }

        [Fragment]
        public void Update(string? configuration = "localhost:6379", Action<ConfigurationOptions>? configure = null, int database = -1, SerializationFormat serializationFormat = SerializationFormat.MessagePack, bool connectAsync = true)
        {
            var client = _redisClientManager.Update(configuration, configure, database, serializationFormat, connectAsync);
            if (client != _redisClient)
            {
                _redisClient = client;
                if (client != null)
                    UpdateBindingsFromModel(_modelStream.Value ?? ImmutableDictionary<string, BindingModel>.Empty);
            }
        }

        [Fragment]
        public RedisClient? Client => _redisClient;

        [Fragment]
        public bool IsConnected => _redisClientManager.IsConnected;

        [Fragment]
        public string ClientName => _redisClientManager.ClientName;

        // Called from patched ModuleView
        public void AddBinding(string channelPath, IChannel channel, BindingModel bindingModel)
        {
            // Save in our pin - this will trigger a sync
            var model = _modelStream.Value ?? ImmutableDictionary<string, BindingModel>.Empty;
            _modelStream.Value = model.SetItem(channelPath, bindingModel);
        }

        // Called from patched ModuleView
        public void RemoveBinding(IRedisBinding binding)
        {
            var model = _modelStream.Value ?? ImmutableDictionary<string, BindingModel>.Empty;
            if (binding.ChannelName != null)
            {
                var updatedModel = model.Remove(binding.ChannelName);
                if (updatedModel != model)
                    _modelStream.Value = updatedModel;
            }
        }

        private void UpdateBindingsFromModel(ImmutableDictionary<string, BindingModel> model)
        {
            if (_redisClient is null)
            {
                _logger.LogWarning("No active Redis client. Can't sync bindings.");
                return;
            }

            // Cleanup
            var obsoleteBindings = new List<IRedisBinding>();
            foreach (var binding in _redisClient.Bindings)
            {
                if (binding.Module != this)
                    continue;

                var key = binding.Model.Key;
                if (model.TryGetValue(key, out var bindingModel))
                {
                    // Did the model change?
                    if (binding.Model != bindingModel)
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

                if (_redisClient.TryGetBinding(bindingModel.Key, out var existingBinding))
                {
                    if (existingBinding.Module is null || existingBinding.Module != this)
                    {
                        _logger.LogError("Failed to add Redis binding for \"{channelName}\" because it is already bound", channelName);
                    }
                    continue;
                }

                try
                {
                    _redisClient.AddBinding(bindingModel, channel, this, channelName: channelName);
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
    }
}
