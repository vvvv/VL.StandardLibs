using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Linq;
using System.Reactive.Linq;
using VL.Core;
using VL.Lib.Collections;

namespace VL.IO.Redis
{
    internal class RedisConnection : IDisposable
    {
        private readonly ConnectionMultiplexer _multiplexer;
        private readonly RedisClient _client;
        private readonly ILogger _logger;

        public RedisConnection(AppHost appHost, ConnectionMultiplexer multiplexer, ILogger logger, RedisClient client)
        {
            _multiplexer = multiplexer;
            _client = client;
            _logger = logger;

            // Subscribe to connection error events
            _multiplexer.ConnectionFailed += OnConnectionFailed;
            _multiplexer.ErrorMessage += OnErrorMessage;
            _multiplexer.InternalError += OnInternalError;

            try
            {
                // This opens a Pub/Sub connection internally
                var subscriber = multiplexer.GetSubscriber();
                var _invalidations = subscriber.Subscribe(RedisChannel.Literal("__redis__:invalidate"));
                _invalidations.OnMessage(msg =>
                {
                    try
                    {
                        client.OnInvalidationMessage(msg);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing invalidation message");
                    }
                });

                EnableClientSideTracking();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Redis subscription");
            }

            _multiplexer.ConnectionRestored += (s, e) =>
            {
                try
                {
                    // Re-enable client side tracking
                    EnableClientSideTracking();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to re-enable client tracking after connection restored");
                }
            };
        }

        private void EnableClientSideTracking()
        {
            // https://medium.com/@darali7575/understanding-client-tracking-in-redis-43215e1495c1
            // HACK: It seems the StackExchange API is a little too high level here / doesn't support this yet properly:
            // 1) CLIENT TRACKING ON without the REDIRECT option requires RESP3, but StackExchange will crash in that case not being able to handle the incoming server message
            // 2) CLIENT TRACKING ON with the REDIRECT option only seems to work in RESP2, but in RESP2 we need to use a 2nd connection for Pub/Sub.
            //    However getting the ID of that 2nd connection is not possible in StackExchange (https://stackoverflow.com/questions/66964604/how-do-i-get-the-client-id-for-the-isubscriber-connection)
            //    we need to ask the server (requires the AllowAdmin option) for the client list and then identify our pub/sub connection.
            // Hopefully the situation should improve once https://github.com/StackExchange/StackExchange.Redis/tree/server-cache-invalidation is merged back.
            try
            {
                foreach (var s in _multiplexer.GetServers())
                {
                    if (!s.IsConnected)
                        continue;

                    try
                    {
                        var pubSubClient = s.ClientList().FirstOrDefault(c => c.Name == _multiplexer.ClientName && c.ClientType == ClientType.PubSub);
                        if (pubSubClient != null)
                        {
                            s.Execute("CLIENT", new object[] { "TRACKING", "ON", "REDIRECT", pubSubClient.Id.ToString(), "BCAST", "NOLOOP" });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to enable client tracking on server {EndPoint}", s.EndPoint);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enable client-side tracking");
            }
        }

        private void OnConnectionFailed(object? sender, ConnectionFailedEventArgs e)
        {
            _logger.LogError(e.Exception, "Redis connection failed: {FailureType} on {EndPoint}", e.FailureType, e.EndPoint);
        }

        private void OnErrorMessage(object? sender, RedisErrorEventArgs e)
        {
            _logger.LogError("Redis error message: {Message} on {EndPoint}", e.Message, e.EndPoint);
        }

        private void OnInternalError(object? sender, InternalErrorEventArgs e)
        {
            _logger.LogError(e.Exception, "Redis internal error: {Origin} on {EndPoint}", e.Origin, e.EndPoint);
        }

        public void Dispose()
        {
            _multiplexer.ConnectionFailed -= OnConnectionFailed;
            _multiplexer.ErrorMessage -= OnErrorMessage;
            _multiplexer.InternalError -= OnInternalError;
            _multiplexer.Dispose();
        }

        public string ClientName => _multiplexer.ClientName;

        internal IDatabase GetDatabase(int db = -1) => _multiplexer.GetDatabase(db);

        internal IServer? GetServer() => _multiplexer.GetServers().FirstOrDefault();

        internal ConnectionMultiplexer Multiplexer => _multiplexer;

        internal RedisClient Client => _client;

        internal ISubscriber GetSubscriber() => _multiplexer.GetSubscriber();
    }
}
