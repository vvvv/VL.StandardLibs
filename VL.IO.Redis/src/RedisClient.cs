using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using VL.Core;
using VL.Core.Import;
using VL.IO.Redis.Internal;
using VL.Lib.Animation;
using VL.Lib.Collections;
using VL.Lib.Reactive;
using VL.Serialization.MessagePack;
using VL.Serialization.Raw;

[assembly: ImportAsIs(Namespace = "VL")]

namespace VL.IO.Redis
{
    // TODO: We want to hide the operations of this class
    public class RedisClient : IDisposable
    {
        private readonly TransactionBuilder _transactionBuilder = new();
        private readonly ILogger _logger;
        internal readonly Dictionary<string, (BindingModel model, IRedisBinding binding)> _bindings = new();
        private readonly ConnectionMultiplexer _multiplexer;
        private readonly Subject<Unit> _networkSync = new Subject<Unit>();
        private readonly AppHost _appHost;

        private ImmutableArray<IParticipant> _participants = ImmutableArray<IParticipant>.Empty;

        private Task? _lastTransaction;

        public RedisClient(ConnectionMultiplexer multiplexer, ILogger logger)
        {
            _multiplexer = multiplexer;
            _logger = logger;

            // Capture the current app host - we'll need it later when serializing values
            _appHost = AppHost.Current;

            // This opens a Pub/Sub connection internally
            var subscriber = multiplexer.GetSubscriber();
            var _invalidations = subscriber.Subscribe(RedisChannel.Literal("__redis__:invalidate"));
            _invalidations.OnMessage(OnInvalidationMessage);

            EnableClientSideTracking();
            _multiplexer.ConnectionRestored += (s, e) =>
            {
                // Re-enable client side tracking
                EnableClientSideTracking();
            };
        }

        private void EnableClientSideTracking()
        {
            // HACK: It seems the StackExchange API is a little too high level here / doesn't support this yet properly:
            // 1) CLIENT TRACKING ON without the REDIRECT option requires RESP3, but StackExchange will crash in that case not being able to handle the incoming server message
            // 2) CLIENT TRACKING ON with the REDIRECT option only seems to work in RESP2, but in RESP2 we need to use a 2nd connection for Pub/Sub.
            //    However getting the ID of that 2nd connection is not possible in StackExchange (https://stackoverflow.com/questions/66964604/how-do-i-get-the-client-id-for-the-isubscriber-connection)
            //    we need to ask the server (requires the AllowAdmin option) for the client list and then identify our pub/sub connection.
            // Hopefully the situation should improve once https://github.com/StackExchange/StackExchange.Redis/tree/server-cache-invalidation is merged back.
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

        public string ClientName => _multiplexer.ClientName;

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

        internal IDisposable AddBinding(BindingModel model, IChannel channel, RedisModule? module = null, ILogger? logger = null)
        {
            if (_bindings.ContainsKey(model.Key))
                throw new InvalidOperationException($"The Redis key \"{model.Key}\" is already bound to a different channel.");

            var binding = (IRedisBinding)Activator.CreateInstance(
                type: typeof(Binding<>).MakeGenericType(channel.ClrTypeOfValues),
                args: new object?[] { this, channel, model, module, logger })!;
            _bindings[model.Key] = (model, binding);
            return Disposable.Create(() =>
            {
                _bindings.Remove(model.Key);
                binding.Dispose();
            });
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
}
