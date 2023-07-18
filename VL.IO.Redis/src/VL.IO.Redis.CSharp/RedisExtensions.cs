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
using System.Threading.Channels;
using System.Transactions;

namespace VL.IO.Redis
{
    
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

        public static IObservable<ImmutableDictionary<Guid, object>> ObservableTransactions(this ITransaction transaction, IList<Task<KeyValuePair<Guid, object>>> _tasks)
        {
            return Observable.Create<ImmutableDictionary<Guid, object>>(async (obs, ct) =>
            {
                // as the SubscribeAsync callback can be invoked concurrently
                // a thread-safe wrapper for OnNext is needed
                var syncObs = Observer.Synchronize(obs);
                var tmp = await transaction.ExecuteAsync().ContinueWith
                (
                    async t => await Task.WhenAll(_tasks).ContinueWith(t => t.Result.ToImmutableDictionary())
                );

                tmp.GetAwaiter().OnCompleted(() => { syncObs.OnNext(tmp.GetAwaiter().GetResult()); });


                return tmp;// Disposable.Create(() => subscriber.Unsubscribe(channel));
            });
        }

    }

   
}
