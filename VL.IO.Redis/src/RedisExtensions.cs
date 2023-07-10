using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Collections.Immutable;
using System.Reactive.Concurrency;

namespace VL.IO.Redis
{
    public class RedisCommandQueue
    {
        private ITransaction _tran;
        private readonly IList<Task<KeyValuePair<Guid, object>>> _tasks = new List<Task<KeyValuePair<Guid, object>>>();

        private Task<Task<ImmutableDictionary<Guid, object>>> _result;

        public int Count => _tasks.Count;
        public RedisCommandQueue Enqueue(Func<ITransaction, Task<KeyValuePair<Guid, object>>> cmd)
        {
            if (_tran != null)
                _tasks.Add(cmd(_tran));
            return this;
        }

        public void Enqueue(IList<Func<ITransaction, Task<KeyValuePair<Guid, object>>>> cmd)
        {
            if (_tran != null)
                foreach(var c in cmd)
                    _tasks.Add(c(_tran));
            cmd.Clear();
        }

        public RedisCommandQueue()
        { }

        public void BeginTransaction(IDatabase database)
        {
            _tasks.Clear();
            _tran = database.CreateTransaction();
        }

        public void ExecuteAsync(out int Count)
        {
            Count = _tasks.Count;
            _result = _tran.ExecuteAsync().ContinueWith(
            t =>
                Task.WhenAll(_tasks)
                .ContinueWith(t => new Dictionary<Guid, object>(t.Result)
                .ToImmutableDictionary())
            );
        }

        public ImmutableDictionary<Guid, object> Result()
        {
            return _result.ConfigureAwait(false).GetAwaiter().GetResult().ConfigureAwait(false).GetAwaiter().GetResult();

        }
    }
    public sealed class ThreadSafeToggle
    {
        public ThreadSafeToggle() { }

        private bool enabled = true;
        //private object syncObj = new object();

        public void Enable()
        {
            //lock (syncObj)
            {
                enabled = true;
            }
        }
        public void Disable()
        {
            //lock (syncObj)
            {
                enabled = false;
            }
        }
        public bool Enabled()
        {
            //lock (syncObj)
            {
                return enabled;
            }
        }
    }

    public static class RedisExtensions
    {

        public static Task<KeyValuePair<Guid, object>> Cast<T>(this Task<T> task, Guid guid)
        {
            return task.ContinueWith(t => new KeyValuePair<Guid,object>(guid, (object)t.Result));
        }

        public static IObservable<RedisValue> WhenMessageReceived(this ISubscriber subscriber, RedisChannel channel)
        {
            return Observable.Create<RedisValue>(async (obs, ct) =>
            {
                // as the SubscribeAsync callback can be invoked concurrently
                // a thread-safe wrapper for OnNext is needed
                var syncObs = Observer.Synchronize(obs);
                await subscriber.SubscribeAsync(channel, (_, message) =>
                {
                    syncObs.OnNext(message);
                }).ConfigureAwait(false);

                return Disposable.Create(() => subscriber.Unsubscribe(channel));
            });
        }
    }

   
}
