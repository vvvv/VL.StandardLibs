using StackExchange.Redis;
using System;
using System.Threading.Tasks;
using VL.Core;

namespace VL.IO.Redis.Advanced
{
    public static class RedisClientExtensions
    {
        /// <summary>
        /// Get the value of key. If the key does not exist the <paramref name="defaultValue"/> is returned.
        /// An error is returned if the value stored at key is not a string, because GET only handles string values.
        /// </summary>
        /// <param name="client">The Redis client.</param>
        /// <param name="key">The key of the string.</param>
        /// <param name="format">The serialization format to use. If not provided the one from the <paramref name="client"/> will be used.</param>
        /// <param name="defaultValue">The value to return in case the key does not exist or the client is not connected.</param>
        /// <returns>The value of key, or <paramref name="defaultValue"/> when key does not exist.</returns>
        /// <remarks><seealso href="https://redis.io/commands/get"/></remarks>
        public static Optional<T> Get<T>(this RedisClient client, 
            string key, 
            Optional<SerializationFormat> format,
            Optional<T> defaultValue)
        {
            if (client is null) throw new ArgumentNullException(nameof(client));

            var internalClient = client.InternalRedisClient;
            if (internalClient is null)
                return defaultValue;

            var db = internalClient.GetDatabase();
            if (db is null)
                return defaultValue;

            var redisValue = db.StringGet(key);
            if (redisValue.IsNull)
                return defaultValue;

            return client.Deserialize<T>(redisValue, format.ToNullable())!;
        }

        /// <summary>
        /// Get the value of key. If the key does not exist the <paramref name="defaultValue"/> is returned.
        /// An error is returned if the value stored at key is not a string, because GET only handles string values.
        /// </summary>
        /// <param name="client">The Redis client.</param>
        /// <param name="key">The key of the string.</param>
        /// <param name="format">The serialization format to use. If not provided the one from the <paramref name="client"/> will be used.</param>
        /// <param name="defaultValue">The value to return in case the key does not exist or the client is not connected.</param>
        /// <returns>The value of key, or <paramref name="defaultValue"/> when key does not exist.</returns>
        /// <remarks><seealso href="https://redis.io/commands/get"/></remarks>
        public static async Task<Optional<T>> GetAsync<T>(this RedisClient client,
            string key,
            Optional<SerializationFormat> format,
            Optional<T> defaultValue)
        {
            if (client is null) throw new ArgumentNullException(nameof(client));

            var internalClient = client.InternalRedisClient;
            if (internalClient is null)
                return defaultValue;

            var db = internalClient.GetDatabase();
            if (db is null)
                return defaultValue;

            var redisValue = await db.StringGetAsync(key).ConfigureAwait(false);
            if (redisValue.IsNull)
                return defaultValue;

            return client.Deserialize<T>(redisValue, format.ToNullable())!;
        }

        /// <inheritdoc cref="IDatabase.StringSet(RedisKey, RedisValue, TimeSpan?, bool, When, CommandFlags)"/>
        public static bool Set<T>(this RedisClient client,
            string key,
            T value,
            Optional<SerializationFormat> format,
            Optional<TimeSpan> expiry,
            When when)
        {
            if (client is null) throw new ArgumentNullException(nameof(client));

            var internalClient = client.InternalRedisClient;
            if (internalClient is null)
                return false;

            var db = internalClient.GetDatabase();
            if (db is null)
                return false;

            var redisValue = client.Serialize(value, format.ToNullable());
            return db.StringSet(key, redisValue, expiry.ToNullable(), when: when);
        }

        /// <inheritdoc cref="IDatabaseAsync.StringSetAsync(RedisKey, RedisValue, TimeSpan?, bool, When, CommandFlags)"/>
        public static async Task<bool> SetAsync<T>(this RedisClient client, 
            string key, 
            T value, 
            Optional<SerializationFormat> format,
            Optional<TimeSpan> expiry,
            When when)
        {
            if (client is null) throw new ArgumentNullException(nameof(client));

            var internalClient = client.InternalRedisClient;
            if (internalClient is null)
                return false;

            var db = internalClient.GetDatabase();
            if (db is null)
                return false;

            var redisValue = client.Serialize(value, format.ToNullable());
            return await db.StringSetAsync(key, redisValue, expiry.ToNullable(), when: when).ConfigureAwait(false);
        }
    }
}
