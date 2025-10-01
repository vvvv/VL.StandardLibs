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
    }
}
