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
        private Task<bool> _successfulTask;
        private bool _successful;
        private Task<object[]> _result;

        public RedisCommandQueue Enqueue(Func<ITransaction, Task<object>> cmd)
        {
            if (_tran != null)
                _tasks.Add(cmd(_tran));
            return this;
        }

        public RedisCommandQueue(ITransaction tran) => _tran = tran;

        public void ExecuteAsync()
        {
            _successfulTask = _tran.ExecuteAsync();
        }

        public bool AwaitExecute(int timeout = 16)
        {
            if (_successfulTask.Wait(timeout))
            {
                _result = Task.WhenAll(_tasks);
                _successful = true;
                return true;
            }
            else 
            {
                _successful = false;
                return false; 
            }
        }

        public object[] Result(int timeout = 16)
        {
            if (_successful)
                if (_result.Wait(timeout))
                    return _result.Result;
            return new object[0];
        }

        public async Task<object[]> Execute()
        {
            if (await _tran.ExecuteAsync())
                return await Task.WhenAll(_tasks);
            else
                return new object[0];
        }
    }
    public static class RedisExtensions
    {

        public static Task<object> Cast<T>(this Task<T> task)
        {
            return task.ContinueWith(t => (object)t.Result);
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
