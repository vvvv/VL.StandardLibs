using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive;
using System.Reactive.Disposables;

namespace VL.IO.Redis
{
    public class RedisCommandQueue
    {
        private readonly ITransaction _tran;
        private readonly IList<Task<object>> _tasks = new List<Task<object>>();

        public RedisCommandQueue Enqueue(Func<ITransaction, Task<object>> cmd)
        {
            _tasks.Add(cmd(_tran));
            return this;
        }

        public RedisCommandQueue(ITransaction tran) => _tran = tran;

        public async Task<object[]> Execute()
        {
            if (await _tran.ExecuteAsync())
                return await Task.WhenAll(_tasks);
            else
                return null;
        }
    }
    public static class RedisExtensions
    {
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
