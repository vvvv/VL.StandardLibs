using VL.Lib.Collections;

namespace VL.IO.Redis
{
    public static class ServerManagement
    {
        public static RedisClient? DeleteKey(this RedisClient? client, string? key, bool apply, out bool success)
        {
            success = false;

            if (!apply || client is null || string.IsNullOrEmpty(key))
                return client;

            success = client.GetDatabase().KeyDelete(key);

            return client;
        }

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

        public static RedisClient? Scan(this RedisClient? client, string? pattern, bool apply, out Spread<string> keys)
        {
            keys = Spread<string>.Empty;

            if (!apply)
                return client;

            if (client is null)
                return client;

            var server = client.GetServer();
            if (server is null)
                return client;

            var builder = new SpreadBuilder<string>();
            foreach (var key in server.Keys(client.Database, pattern))
                builder.Add(key.ToString());

            keys = builder.ToSpread();

            return client;
        }
    }
}
