using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace VL.IO.Redis
{
    public record RedisCommandQueue
    {
        private ITransaction _tran;
        private IList<Task<KeyValuePair<Guid, object>>> _tasks;
        private IList<Func<ITransaction, Task<KeyValuePair<Guid, object>>>> _cmds;
        private List<RedisKey> _changedkeys;

        public void Enqueue(Func<ITransaction, Task<KeyValuePair<Guid, object>>> cmd )
        {
            _cmds.Add(cmd);
        }

        public void ChangedKeys(IEnumerable<RedisKey> keys)
        {
            _changedkeys.AddRange(keys);
        }

        public RedisCommandQueue(IDatabase database)
        {
            _tran           = database.CreateTransaction();
            _tasks          = new List<Task<KeyValuePair<Guid, object>>>();
            _cmds           = new List<Func<ITransaction, Task<KeyValuePair<Guid, object>>>>();
            _changedkeys    = new List<RedisKey>();
        }


        public void AddTransactions(out int Count, out ImmutableList<RedisKey> Changes)
        {
            try
            {
                if (_tran != null)
                    foreach (var cmd in _cmds.ToImmutableList())
                        _tasks.Add(cmd(_tran));
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex);
            }
            
            Count = _tasks.Count;
            Changes = _changedkeys.ToImmutableList();
        }

        public static IObservable<RedisCommandQueue> ApplyTransactions(IObservable<RedisCommandQueue> observable)
        {
            return Observable.Create<RedisCommandQueue>((obs) =>
            {
                var syncObs = Observer.Synchronize(obs, true);
                return observable.Subscribe(async
                (queue) =>
                {
                    if (queue._tran != null)
                    {

                    }
                }
                ,(ex) =>
                {
                    Console.WriteLine(ex);
                    syncObs.OnError(ex);
                }
                ,() =>
                {
                    Console.WriteLine("COMPLETED");
                    syncObs.OnCompleted();
                });
            });
        }


        public static IObservable<ImmutableDictionary<Guid, object>> ExecuteTransaction(IObservable<RedisCommandQueue> observable, Action<float> action)
        {

            return Observable.Create<ImmutableDictionary<Guid, object>>((obs) =>
            {
                var syncObs = Observer.Synchronize(obs, true);
                return observable.Subscribe(async 
                    (queue) =>
                    {
                        try
                        {
                            var sw = Stopwatch.StartNew();

                            if (await queue._tran.ExecuteAsync())
                            {
                                try
                                {
                                    var resultAwaiter = Task.WhenAll(queue._tasks).GetAwaiter();

                                    resultAwaiter.OnCompleted(() =>
                                    {
                                        action.Invoke((float)sw.ElapsedTicks / (float)(TimeSpan.TicksPerMillisecond / 1000));

                                        ImmutableDictionary<Guid, object>.Builder builder = ImmutableDictionary.CreateBuilder<Guid, object>();
                                        foreach (var kv in resultAwaiter.GetResult())
                                        {
                                            builder.TryAdd(kv.Key, kv.Value);
                                        }
                                        syncObs.OnNext(builder.ToImmutable());
                                    });
                                }
                                catch (Exception ex) 
                                {
                                    Console.WriteLine(ex);
                                    syncObs.OnError(ex);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                            syncObs.OnError(ex);
                        }
                    }
                    ,(ex) => 
                    {
                        Console.WriteLine(ex);
                        syncObs.OnError(ex); 
                    }
                    ,() =>
                    {
                        Console.WriteLine("COMPLETED");
                        syncObs.OnCompleted();
                    }
                );
            });
        }

    }
}
