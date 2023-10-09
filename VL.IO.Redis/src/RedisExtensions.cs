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
        public static Guid getID(this RedisCommandQueue queue) { return queue.id; }

        public static ValueTuple<RedisCommandQueue, TInput> Enqueue<TInput, TOutput>
        (
            ValueTuple<RedisCommandQueue, TInput> input,
            Func<ITransaction, TInput, Task<TOutput>> cmd,
            Guid guid,
            Optional<Func<TInput, IEnumerable<string>>> keys
        )
        {
            if (input.Item1.Transaction != null)
            {
                input.Item1.Cmds.Enqueue(
                    (tran) => ValueTuple.Create
                    (
                        cmd.Invoke(tran, input.Item2).ContinueWith(t => new KeyValuePair<Guid, object>(guid, (object)t.Result)),
                        keys.HasValue ? keys.Value.Invoke(input.Item2).Select(k => new RedisKey(k)) : Enumerable.Empty<RedisKey>()
                    )   
                );
            }
            return input;
        }

        public static RedisCommandQueue AddReceivedChanges(this RedisCommandQueue queue, Spread<string> receivedChanges)
        {
            queue.ReceivedChanges.UnionWith(receivedChanges);
            return queue;
        }

        public static ImmutableHashSet<string> GetReceivedChanges(this RedisCommandQueue queue)
        {
            return queue.ReceivedChanges.ToImmutable();
        }

        public static IObservable<RedisCommandQueue> ApplyTransactions(this IObservable<RedisCommandQueue> observable, Action<float, int> action, Func<ImmutableHashSet<string>,Tuple<RedisValue,string,RedisChannel.PatternMode,bool>> publishChanges)
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

                        if (queue.Transaction == null && queue.Multiplexer.IsConnected)
                        {
                            return;
                        }
                        foreach (var cmd in queue.Cmds)
                        {
                            var taskKey = cmd(queue.Transaction);
                            queue.Tasks.Enqueue(taskKey.Item1);
                            queue.Changes.UnionWith(taskKey.Item2.Select(v => v.ToString()));
                        }
                        if (!queue.Changes.IsEmpty())
                        {
                            var p = publishChanges.Invoke(queue.Changes.ToImmutable());
                            queue.Tasks.Enqueue(queue.Transaction.PublishAsync(new RedisChannel(p.Item2 + "_" + queue.id.ToString(), p.Item3), p.Item1).ContinueWith(t => new KeyValuePair<Guid, object>(queue.id, (object)t.Result)));
                        }
                        action.Invoke((float)sw.ElapsedTicks / (float)(TimeSpan.TicksPerMillisecond), queue.Tasks.Count);

                        syncObs.OnNext(queue);
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

            ImmutableDictionary<Guid, object>.Builder builder = ImmutableDictionary.CreateBuilder<Guid, object>();

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

                            if (queue.Transaction != null && queue.Multiplexer.IsConnected)
                            {
                                
                                    try
                                    {
                                        bool succsess = false;
                                        try
                                        {
                                            succsess = await queue.Transaction.ExecuteAsync();
                                        }
                                        catch (Exception ex) { }
                                    
                                        if (succsess)
                                        {

                                            var resultAwaiter = Task.WhenAll(queue.Tasks).GetAwaiter();

                                            resultAwaiter.OnCompleted(() =>
                                            {
                                                builder.Clear(); 
                                                foreach (var kv in resultAwaiter.GetResult())
                                                {
                                                    builder.TryAdd(kv.Key, kv.Value);

                                                }
                                                syncObs.OnNext(builder.ToImmutable());

                                                action.Invoke((float)sw.ElapsedTicks / (float)(TimeSpan.TicksPerMillisecond));
                                            });
                                        }
                                        else
                                        {
                                            Console.WriteLine("TransactionFailed");
                                        }
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
