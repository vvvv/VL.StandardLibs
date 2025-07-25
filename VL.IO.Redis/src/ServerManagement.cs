using System;
using System.ComponentModel;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using VL.Core.Import;
using VL.Core.Utils;
using VL.Lib.Collections;

namespace VL.IO.Redis
{
    public static class ServerManagement
    {
        /// <summary>
        /// Deletes a key from the database
        /// </summary>
        /// <param name="client"></param>
        /// <param name="key"></param>
        /// <param name="apply"></param>
        /// <param name="success"></param>
        /// <returns></returns>
        public static RedisClient? DeleteKey(this RedisClient? client, string? key, bool apply, out bool success)
        {
            success = false;

            if (!apply || client is null || string.IsNullOrEmpty(key))
                return client;

            success = client.GetDatabase().KeyDelete(key);

            return client;
        }

        /// <summary>
        /// Removes all keys from the database
        /// </summary>
        /// <param name="client"></param>
        /// <param name="apply"></param>
        /// <returns></returns>
        public static RedisClient? FlushDB(this RedisClient? client, bool apply)
        {
            if (!apply || client is null)
                return client;

            var server = client.GetServer();
            if (server is null)
                return client;

            server.FlushDatabase(client.Database);

            return client;
        }
        /// <summary>
        /// Returns keys available in the database
        /// </summary>
        [ProcessNode(Name = "Scan")]
        [Obsolete("Use ScanAsync instead")]
        public class ScanNode
        {
            private RedisClient? _client;
            private string? _pattern;
            private Spread<string> _keys = Spread<string>.Empty;

            [return: Pin(Name = "Client")]
            public RedisClient? Update(RedisClient? client, string? pattern, bool force, out Spread<string> keys)
            {
                if (force || client != _client || pattern != _pattern)
                {
                    _client = client;
                    _pattern = pattern;
                    _keys = Scan(client, pattern);
                }

                keys = _keys;

                return client;
            }

            private Spread<string> Scan(RedisClient? client, string? pattern)
            {
                if (client is null)
                    return Spread<string>.Empty;

                var server = client.GetServer();
                if (server is null)
                    return Spread<string>.Empty;

                var builder = CollectionBuilders.GetBuilder(_keys, 0);
                foreach (var key in server.Keys(client.Database, pattern))
                    builder.Add(key.ToString());
                return builder.Commit();
            }
        }

        [ProcessNode(Name = "ScanAsync")]
        public class ScanAsyncNode
        {
            private RedisClient? _client;
            private string? _pattern;
            private IObservable<Spread<string>> _keys = Observable.Empty<Spread<string>>();
            private Spread<string> _keysCache = Spread<string>.Empty;
            private bool _stopPollingOnceConnected;

            [return: Pin(Name = "Client")]
            public IObservable<Spread<string>> Update(RedisClient? client, string? pattern, [DefaultValue(1f)] float period, bool stopPollingOnceConnected, bool force)
            {
                if (client != _client || pattern != _pattern || stopPollingOnceConnected != _stopPollingOnceConnected || force)
                {
                    _client = client;
                    _pattern = pattern;
                    _stopPollingOnceConnected = stopPollingOnceConnected;
                    _keys = Observable.Create<Spread<string>>(async (observer, token) =>
                    {
                        var delay = TimeSpan.FromSeconds(Math.Max(period, 0.1));
                        while (!token.IsCancellationRequested)
                        {
                            var server = client?.GetServer();
                            if (client != null && server != null)
                            {
                                try
                                {
                                    var builder = CollectionBuilders.GetBuilder(_keysCache, 0);
                                    await foreach (var key in server.KeysAsync(client.Database, pattern).WithCancellation(token))
                                        builder.Add(key.ToString());
                                    observer.OnNext(_keysCache = builder.Commit());

                                    while (stopPollingOnceConnected && !token.IsCancellationRequested && client.Multiplexer.IsConnected)
                                    {
                                        await Task.Delay(delay, token);
                                    }
                                }
                                catch (Exception)
                                {
                                }
                            }
                            await Task.Delay(delay, token);
                        }
                    }).SubscribeOn(Scheduler.Default);
                }

                return _keys;
            }
        }
    }
}
