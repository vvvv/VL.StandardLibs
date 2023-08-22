using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive;
using System.Collections.Immutable;
using System.Reactive.Concurrency;
using System.Diagnostics;
using VL.Core;
using VL.Lib.Collections;

namespace VL.IO.Redis
{
    public static class RedisExtensions
    {
        

        public static ValueTuple<RedisCommandQueue, TInput> Enqueue<TInput,TOutput>(ValueTuple<RedisCommandQueue,TInput> input, Func<ITransaction, TInput, Task<TOutput>> cmd, Guid guid, Optional<Func<TInput,IEnumerable<string>>> keys)
        {
            input.Item1._cmds.Enqueue(
                (tran) => cmd.Invoke(tran, input.Item2)
                    .ContinueWith(
                        t => new KeyValuePair<Guid, object>(guid, (object)t.Result))
            );

            if (keys.HasValue)
            {
                input.Item1._changes.AddRange(keys.Value.Invoke(input.Item2));
     
            }

            return input;
        }

        public static IObservable<RedisCommandQueue> ApplyTransactions(this IObservable<RedisCommandQueue> observable, Action<float, int> action, Func<Spread<string>,Tuple<RedisValue,string,RedisChannel.PatternMode,bool>> publishChanges)
        {
            

            return Observable.Create<RedisCommandQueue>((obs) =>
            {
                var syncObs = Observer.Synchronize(obs, true);
                return observable.Subscribe(
                (queue) =>
                {
                    try
                    {
                        var sw = Stopwatch.StartNew();

                        if (queue._tran != null)
                        {
                            if (!queue._changes.IsEmpty())
                            {
                                var p = publishChanges.Invoke(queue._changes.ToSpread());
                                if (p.Item4)
                                {
                                    queue._tasks.Add(queue._tran.PublishAsync(new RedisChannel(p.Item2 + "_" + queue._id.ToString(), p.Item3), p.Item1).ContinueWith(t => new KeyValuePair<Guid, object>(queue._id, (object)t.Result)));
                                    //queue._tasks.Add(queue._tran.PublishAsync(new RedisChannel(p.Item2 + "_" + guid.ToString(), p.Item3), p.Item1).ContinueWith(t => new KeyValuePair<Guid, object>(guid, (object)t.Result)));
                                    //queue._tasks.Add(queue._tran.PublishAsync(new RedisChannel(p.Item2 + "_" + guid.ToString(), p.Item3), p.Item1).ContinueWith(t => new KeyValuePair<Guid, object>(guid, (object)t.Result)));
                                } 
                            }
                                
                            foreach (var cmd in queue._cmds)
                            {
                                queue._tasks.Add(cmd(queue._tran));
                            }
                            action.Invoke((float)sw.ElapsedTicks / (float)(TimeSpan.TicksPerMillisecond), queue._tasks.Count);

                            syncObs.OnNext(queue);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        syncObs.OnError(ex);
                    }
                },
                (ex) =>
                {
                    Console.WriteLine(ex);
                    syncObs.OnError(ex);
                },
                () =>
                {
                    Console.WriteLine("COMPLETED");
                    syncObs.OnCompleted();
                });
            });
        }


        public static IObservable<ImmutableDictionary<Guid, object>> ExecuteTransaction(this IObservable<RedisCommandQueue> observable, Action<float> action, IScheduler scheduler)
        {

            return Observable.Create<ImmutableDictionary<Guid, object>>((obs) =>
            {
                //var syncObs = Observer.Synchronize(obs, true);
                var syncObs = Observer.NotifyOn(obs, scheduler);

                return observable.Subscribe(async
                    // onNext
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
                                    

                                    ImmutableDictionary<Guid, object>.Builder builder = ImmutableDictionary.CreateBuilder<Guid, object>();
                                    foreach (var kv in resultAwaiter.GetResult())
                                    {
                                        builder.TryAdd(kv.Key, kv.Value);
                                    }
                                    syncObs.OnNext(builder.ToImmutable());
                                    action.Invoke((float)sw.ElapsedTicks / (float)(TimeSpan.TicksPerMillisecond));
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
                    // onError
                    , (ex) =>
                    {
                        Console.WriteLine(ex);
                        syncObs.OnError(ex);
                    }
                    // onComplete
                    , () =>
                    {
                        Console.WriteLine("COMPLETED");
                        syncObs.OnCompleted();
                    }
                );
            });
        }
    }
}
