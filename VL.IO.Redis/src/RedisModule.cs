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

        [Fragment]
        public bool IsConnected => _redisClientManager.IsConnected;

        [Fragment]
        public string ClientName => _redisClientManager.ClientName;

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
            {
                var binding = entry.Value.binding;
                if (binding.Module != this)
                    continue;

                if (model.TryGetValue(entry.Key, out var bindingModel))
                {
                    // Did the model change?
                    if (binding.Model != bindingModel)
                        obsoleteKeys.Add(entry.Key);
                }
                else
                {
                    // Deleted
                    obsoleteKeys.Add(entry.Key);
                }
            }

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
                if (!_redisClient._bindings.ContainsKey(key))
                    _redisClient.AddBinding(bindingModel, channel, this);
            }
        }

        internal void RemoveBinding(IRedisBinding binding)
        {
            _modelStream.Value = _modelStream.Value!.Remove(binding.Model.Key);
        }

        string IModule.Name => "Redis";

        string IModule.Description => "This module binds a channel to a Redis key";

        bool IModule.SupportsType(Type type) => true;
    }
}
