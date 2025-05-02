using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
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
    internal class RedisClientInternal : IDisposable
    {

        private readonly ILogger _logger;

        private readonly ConnectionMultiplexer _multiplexer;
        private readonly Subject<Unit> _networkSync = new Subject<Unit>();
        private readonly AppHost _appHost;
        private readonly RedisClientManager _manager;


        public RedisClientInternal(AppHost appHost, ConnectionMultiplexer multiplexer, ILogger logger, RedisClientManager manager)
        {
            _appHost = appHost;
            _multiplexer = multiplexer;
            _logger = logger;
            _manager = manager;

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
                if (!s.IsConnected)
                    continue;

                var pubSubClient = s.ClientList().FirstOrDefault(c => c.Name == _multiplexer.ClientName && c.ClientType == ClientType.PubSub);
                if (pubSubClient != null)
                {
                    s.Execute("CLIENT", new object[] { "TRACKING", "ON", "REDIRECT", pubSubClient.Id.ToString(), "BCAST", "NOLOOP" });
                }
            }
        }

        public void Dispose()
        {
            _multiplexer.Dispose();
        }

        [DefaultValue(-1)]
        public int Database { internal get; set; } = -1;

        public string ClientName => _multiplexer.ClientName;

        internal Subject<Unit> NetworkSync => _networkSync;

        internal IDatabase GetDatabase() => _multiplexer.GetDatabase(Database);

        internal IServer? GetServer() => _multiplexer.GetServers().FirstOrDefault();

        internal ConnectionMultiplexer Multiplexer => _multiplexer;



        internal ISubscriber GetSubscriber() => _multiplexer.GetSubscriber();

        private void OnInvalidationMessage(ChannelMessage message)
        {
            var key = message.Message.ToString();
            if (key is null)
                return;

            if (_logger.IsEnabled(LogLevel.Trace))
                _logger.LogTrace("Redis invalidated {key}", key);

            foreach (var p in _manager.Participants)
                p.Invalidate(key);
        }
    }
}
