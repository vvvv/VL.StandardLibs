using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VL.IO.Redis
{
    public class RedisClient : IDisposable
    { 
        public IServer Server { get { return server; } }
        public IDatabase Database { get { return database; } }
        public ISubscriber Subscriber { get { return subscriber; } }
        public bool IsConnected { get { return redis.IsConnected; } }



        private bool _disposed;
        private readonly Config config = new Config();
        private readonly ConnectionMultiplexer redis;
        private IServer server;
        private IDatabase database;
        private ISubscriber subscriber;

        // Atomic GetMultiHash
        public RedisResult GetMultiHash(string keypattern)
        {
            return getMultiHash.Evaluate(database, new { @keypattern = keypattern });
        }
        private const string getMultiHashLua =
                @"  local glob = @keypattern
                    local t = { }
                    local keys = redis.call('keys', glob)
                    for iter, value in ipairs(keys) do
                        table.insert(t, { value, redis.call('HGETALL', value) })
                    end
                    return t";
        private LuaScript getMultiHashLuaScript;
        private LoadedLuaScript getMultiHash;

        // Atomic DeleteKeys
        public void DeleteKeys(string keypattern)
        {
            deleteKeys.EvaluateAsync(database, new { @keypattern = keypattern });
        }
        private const string deleteKeysLua =
                @"  local glob = @keypattern
                    local keys = redis.call('keys', glob) 
                    if next(keys) == nil then return 0 end
                    return redis.call('DEL', unpack(keys))
                ";
        private LuaScript deletKeysLuaScript;
        private LoadedLuaScript deleteKeys;

        public RedisClient(Action<IConfig> configuration = null)
        {
            if (configuration != null)
                configuration(config);

            // Connect
            try
            {
                redis = ConnectionMultiplexer.Connect(config.ip + ':' + config.port + ",allowAdmin = true,ConnectRetry = 1");
            }
            catch (Exception e)
            {
                throw e;
            }


            redis.ConnectionFailed += new EventHandler<ConnectionFailedEventArgs>(delegate (Object o, ConnectionFailedEventArgs a)
            {
                //snip
            });

            redis.ConnectionRestored += new EventHandler<ConnectionFailedEventArgs>(delegate (Object o, ConnectionFailedEventArgs a)
            {

            });

            // Get Server
            server = redis.GetServer(config.ip + ':' + config.port);
            // Get Database
            database = redis.GetDatabase();
            // Get subscriber
            subscriber = redis.GetSubscriber();

            // Atomic GetMultiHash
            getMultiHashLuaScript = LuaScript.Prepare(getMultiHashLua);
            getMultiHash = getMultiHashLuaScript.Load(server);

            // Atomic DeleteKeys
            deletKeysLuaScript = LuaScript.Prepare(deleteKeysLua);
            deleteKeys = deletKeysLuaScript.Load(server);


        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            try
            {
                if (disposing)
                {
                    redis.Dispose();
                }

            }
            catch (Exception ex)
            {
            }

            _disposed = true;

        }

        ~RedisClient()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

    }
}
