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
using System.Diagnostics.CodeAnalysis;
using MathNet.Numerics.Distributions;
using VL.Serialization.MessagePack;
using VL.Serialization.Raw;
using System.ComponentModel;

namespace VL.IO.Redis
{
    // Patch calls AddBinding and later RemoveBinding.
    // Both methods will subsequently push a new model.
    // Internally we only need to keep the model in sync with what bindings we have at runtime.
    [ProcessNode(FragmentSelection = FragmentSelection.Explicit, HasStateOutput = true)]
    public sealed class RedisClient : IModule, IDisposable
    {
        private const SolutionUpdateKind JustWriteToThePin = SolutionUpdateKind.Default & ~SolutionUpdateKind.AffectCompilation & ~SolutionUpdateKind.AddToHistory;

        private readonly ILogger _logger;
        private readonly NodeContext _nodeContext;
        private readonly IChannel<ImmutableDictionary<string, BindingModel>> _modelStream;
        private readonly IDisposable _modelSubscription;
        private readonly IChannelHub _channelHub;
        private readonly RedisClientManager _redisClientManager;
        private RedisClientInternal? _redisClient;

        [Fragment]
        public RedisClient(
            [Pin(Visibility = PinVisibility.Hidden)]   NodeContext nodeContext,
            [Pin(Visibility = PinVisibility.Hidden)]   AppHost appHost,
            [Pin(Visibility = PinVisibility.Hidden)]   IFrameClock frameClock,
            [Pin(Visibility = PinVisibility.Hidden)]   IChannelHub channelHub,
            [Pin(Visibility = PinVisibility.Optional)] IChannel<ImmutableDictionary<string, BindingModel>> model)
        {
            _nodeContext = nodeContext;
            _logger = nodeContext.GetLogger();

            _modelStream = model;

            _channelHub = channelHub ?? appHost.Services.GetRequiredService<IChannelHub>();
            _redisClientManager = new RedisClientManager(nodeContext, frameClock);
            _channelHub.RegisterModule(this);

            _modelSubscription = _modelStream
                .ObserveOn(appHost.SynchronizationContext)
                .Subscribe(m =>
                {
                    UpdateBindingsFromModel(m ?? ImmutableDictionary<string, BindingModel>.Empty);
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
            Format = serializationFormat;

            _redisClient = _redisClientManager.Update(configuration, configure, database, serializationFormat, connectAsync);
            UpdateBindingsFromModel(_modelStream.Value ?? ImmutableDictionary<string, BindingModel>.Empty);
        }

        [Fragment]
        public bool IsConnected => _redisClientManager.IsConnected;

        [Fragment]
        public string ClientName => _redisClientManager.ClientName;

        [DefaultValue(SerializationFormat.MessagePack)]
        public SerializationFormat Format { private get; set; } = SerializationFormat.MessagePack;

        internal RedisClientManager Manager => _redisClientManager;

        internal RedisClientInternal? InternalRedisClient => _redisClient;

        // Called from channel browser on UI thread
        void IModule.OnAddOrRemoveBinding(IChannel channel, bool add)
        {
            // Update our model - this will trigger a sync
            var channelPath = channel.Path;
            if (channelPath != null)
            {
                var model = _modelStream.Value ?? ImmutableDictionary<string, BindingModel>.Empty;
                _modelStream.Value = add ? model.SetItem(channelPath, new BindingModel(channelPath)) : model.Remove(channelPath);

                SaveModelToPin(_modelStream.Value);
            }
        }

        private void SaveModelToPin(ImmutableDictionary<string, BindingModel> model)
        {
            var solution = IDevSession.Current?.CurrentSolution
                .SetPinValue(_nodeContext.Path.Stack, "Model", model);
            solution?.Confirm(JustWriteToThePin);
        }

        private void UpdateBindingsFromModel(ImmutableDictionary<string, BindingModel> model)
        {
            // Cleanup
            var obsoleteBindings = new List<IRedisBinding>();
            foreach (var binding in Bindings)
            {
                if (binding.GotCreatedViaNode)
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

                if (TryGetBinding(bindingModel.Key, out var existingBinding))
                {
                    if (existingBinding.Module is null || existingBinding.Module != this)
                    {
                        _logger.LogError("Failed to add Redis binding for \"{channelName}\" because it is already bound", channelName);
                    }
                    continue;
                }

                try
                {
                    AddBinding(bindingModel, channel, gotCreatedViaNode: false, channelName: channelName);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to add Redis binding for \"{channelName}\"", channelName);
                }
            }
        }


        internal RedisValue Serialize<T>(T? value, SerializationFormat? preferredFormat)
        {
            using var _ = _nodeContext.AppHost.MakeCurrent();
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
            using var _ = _nodeContext.AppHost.MakeCurrent();
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


        private readonly Dictionary<string, IRedisBinding> _bindings = new();
        internal IEnumerable<IRedisBinding> Bindings => _bindings.Values;
        internal bool TryGetBinding(string key, [NotNullWhen(true)] out IRedisBinding? binding) => _bindings.TryGetValue(key, out binding);

        internal IRedisBinding AddBinding(BindingModel model, IChannel channel, bool gotCreatedViaNode, ILogger? logger = null, string? channelName = null)
        {
            if (_bindings.ContainsKey(model.Key))
                throw new InvalidOperationException($"The Redis key \"{model.Key}\" is already bound to a different channel.");

            var binding = (IRedisBinding)Activator.CreateInstance(
                type: typeof(Binding<>).MakeGenericType(channel.ClrTypeOfValues),
                args: [this, channel, model, gotCreatedViaNode, logger, channelName])!;
            _bindings[model.Key] = binding;
            return binding;
        }

        internal void RemoveBinding(string key)
        {
            _bindings.Remove(key);
        }

        string IModule.Name => "Redis";

        string IModule.Description => "This module binds a channel to a Redis key";

        bool IModule.SupportsType(Type type) => true;
    }
}
