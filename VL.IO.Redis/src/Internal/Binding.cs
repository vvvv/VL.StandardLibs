using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using VL.Core.Reactive;
using VL.Lib.Collections;
using VL.Lib.Reactive;

namespace VL.IO.Redis.Internal
{
    /// <summary>
    /// Represents a binding of a channel to a Redis key.
    /// </summary>
    internal class Binding<T> : IParticipant, IDisposable, IBinding
    {
        private readonly SerialDisposable _clientSubscription = new();
        private readonly SerialDisposable _channelSubscription = new();
        private readonly string _authorId;
        private readonly RedisClient _client;
        private readonly ILogger? _logger;
        private readonly IChannel<T> _channel;
        private readonly BindingModel _bindingModel;
        private readonly ResolvedBindingModel _resolvedBindingModel;
        private readonly Experimental.RedisModule? _module;

        private bool _initialized;
        private bool _weHaveNewData;
        private bool _othersHaveNewData;

        public Binding(RedisClient client, IChannel<T> channel, ResolvedBindingModel resolvedBindingModel, 
            Experimental.RedisModule? module, ILogger? logger)
        {
            _client = client;
            _logger = logger;
            _channel = channel;
            _bindingModel = resolvedBindingModel.Model;
            _resolvedBindingModel = resolvedBindingModel; 
            _module = module;

            _initialized = _resolvedBindingModel.Initialization == Initialization.None;
            _authorId = GetHashCode().ToString();

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
            _client.RemoveBinding(_resolvedBindingModel.Key);
        }

        public ResolvedBindingModel Model => _resolvedBindingModel;

        void IParticipant.Invalidate(string key)
        {
            if (key == _resolvedBindingModel.Key)
                _othersHaveNewData = true;
        }

        void IParticipant.BuildUp(TransactionBuilder builder)
        {
            var needToReadFromDb = NeedToReadFromDb();
            var needToWriteToDb = NeedToWriteToDb();

            // Consider this binding to be initialized from now on
            _initialized = true;

            if (!needToReadFromDb && !needToWriteToDb)
                return;

            if (needToReadFromDb && needToWriteToDb)
            {
                if (_resolvedBindingModel.CollisionHandling == CollisionHandling.LocalWins)
                    needToReadFromDb = false;
                else if (_resolvedBindingModel.CollisionHandling == CollisionHandling.RedisWins)
                    needToWriteToDb = false;
            }

            builder.Add(async transaction =>
            {
                _weHaveNewData = false;
                _othersHaveNewData = false;

                var key = _resolvedBindingModel.Key;
                if (needToWriteToDb)
                {
                    RedisValue redisValue;
                    try
                    {
                        redisValue = _client.Serialize(_channel.Value, _resolvedBindingModel.SerializationFormat);
                    }
                    catch (Exception ex)
                    {
                        if (_logger is null)
                            throw;

                        _logger.LogError(ex, "Error while serializing");
                        return;
                    }
                    _ = transaction.StringSetAsync(key, redisValue, flags: CommandFlags.FireAndForget, 
                        expiry: _resolvedBindingModel.Expiry, 
                        when: _resolvedBindingModel.When);
                }
                if (needToReadFromDb)
                {
                    var redisValue = await transaction.StringGetAsync(key).ConfigureAwait(false);
                    if (redisValue.HasValue)
                    {
                        T? value;
                        try
                        {
                            value = _client.Deserialize<T>(redisValue, _resolvedBindingModel.SerializationFormat);
                        }
                        catch (Exception ex)
                        {
                            if (_logger is null)
                                throw;

                            _logger.LogError(ex, "Error while deserializing");
                            return;
                        }

                        await _client.NetworkSync.Take(1);

                        _channel.SetValueAndAuthor(value, author: _authorId);
                    }
                }
            });

            bool NeedToReadFromDb()
            {
                var bindingType = _resolvedBindingModel.BindingType;
                if (!bindingType.HasFlag(BindingDirection.In))
                    return false;
                if (_initialized)
                    return _othersHaveNewData;
                return _resolvedBindingModel.Initialization == Initialization.Redis;
            }

            bool NeedToWriteToDb()
            {
                var bindingType = _resolvedBindingModel.BindingType;
                if (!bindingType.HasFlag(BindingDirection.Out))
                    return false;
                if (_initialized)
                    return _weHaveNewData;
                return _resolvedBindingModel.Initialization == Initialization.Local;
            }
        }

        IModule? IBinding.Module => _module;

        string IBinding.ShortLabel => "Redis";

        string? IBinding.Description => _bindingModel.ToString() + Environment.NewLine + Environment.NewLine + _resolvedBindingModel.ToString();

        BindingType IBinding.BindingType
        {
            get
            {
                var bindingType = _resolvedBindingModel.BindingType;
                switch (bindingType)
                {
                    case BindingDirection.In:
                        return BindingType.Receive;
                    case BindingDirection.Out:
                        return BindingType.Send;
                    case BindingDirection.InOut:
                        return BindingType.SendAndReceive;
                    default:
                        return BindingType.None;
                }
            }
        }

        bool IBinding.GotCreatedViaNode => Model.CreatedViaNode;

        object IBinding.ResolvedModel => Model;

        bool IBinding.IsTweaked => !Model.IsDefault;
    }
}
