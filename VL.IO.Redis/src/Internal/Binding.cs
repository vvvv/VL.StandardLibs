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
    internal class Binding<T> : IParticipant, IDisposable, IRedisBinding
    {
        private readonly SerialDisposable _clientSubscription = new();
        private readonly SerialDisposable _channelSubscription = new();
        private readonly string _authorId;
        private readonly RedisClient _client;
        private readonly ILogger? _logger;
        private readonly IChannel<T> _channel;
        private readonly BindingModel _bindingModel;
        private readonly RedisModule? _module;

        private bool _initialized;
        private bool _weHaveNewData;
        private bool _othersHaveNewData;

        public Binding(RedisClient client, IChannel<T> channel, BindingModel bindingModel, RedisModule? module, ILogger? logger)
        {
            _client = client;
            _logger = logger;
            _channel = channel;
            _bindingModel = bindingModel;
            _module = module;

            _initialized = bindingModel.Initialization == Initialization.None;
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

            if (_module != null)
                _module.RemoveBinding(this);
        }

        public BindingModel Model => _bindingModel;

        void IParticipant.Invalidate(string key)
        {
            if (key == _bindingModel.Key)
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
                if (_bindingModel.CollisionHandling == CollisionHandling.LocalWins)
                    needToReadFromDb = false;
                else if (_bindingModel.CollisionHandling == CollisionHandling.RedisWins)
                    needToWriteToDb = false;
            }

            builder.Add(async transaction =>
            {
                _weHaveNewData = false;
                _othersHaveNewData = false;

                var key = _bindingModel.Key;
                if (needToWriteToDb)
                {
                    RedisValue redisValue;
                    try
                    {
                        redisValue = _client.Serialize(_channel.Value, _bindingModel.SerializationFormat);
                    }
                    catch (Exception ex)
                    {
                        if (_logger is null)
                            throw;

                        _logger.LogError(ex, "Error while serializing");
                        return;
                    }
                    _ = transaction.StringSetAsync(key, redisValue, flags: CommandFlags.FireAndForget, expiry: _bindingModel.Expiry);
                }
                if (needToReadFromDb)
                {
                    var redisValue = await transaction.StringGetAsync(key).ConfigureAwait(false);
                    if (redisValue.HasValue)
                    {
                        T? value;
                        try
                        {
                            value = _client.Deserialize<T>(redisValue, _bindingModel.SerializationFormat);
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
}
