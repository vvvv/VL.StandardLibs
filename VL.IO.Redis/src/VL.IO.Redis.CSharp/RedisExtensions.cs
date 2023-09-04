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
using VL.Core.Utils;

namespace VL.IO.Redis
{
    public static class RedisExtensions
    {
        public static Guid getID(this RedisCommandQueue queue) { return queue._id; }

        public static RedisCommandQueue GetWhenChangeReceived<TOutput>
        (
            RedisCommandQueue queue, 
            RedisKey key,
            Func<ITransaction, RedisKey, Task<TOutput>> RedisChangedCommand,
            Guid guid,
            out bool ChangeReceived
        )
        {
            ChangeReceived = queue.ReceivedChangesBuilder.Contains(key) && queue.ReceivedChangesBuilder.Count > 0 ;

            if (queue._tran != null && ChangeReceived) 
            {
                queue.Cmds.Enqueue
                (
                    (tran) => ValueTuple.Create
                    (
                        RedisChangedCommand(tran, key).ContinueWith(t => new KeyValuePair<Guid, object>(guid, (object)t.Result)),
                        Enumerable.Empty<RedisKey>()
                    )
                );
            }
            return queue;
        }


        public static ValueTuple<RedisCommandQueue, KeyValuePair<RedisKey, TInput>> SerializeSetAndPushChanges<TInput, TInputSerialized, TOutput>
        (
            ValueTuple<RedisCommandQueue, KeyValuePair<RedisKey, TInput>> input,
            Func<TInput, TInputSerialized> serialize,
            Func<ITransaction, KeyValuePair<RedisKey, TInputSerialized>, Task<TOutput>> ChannelChangedCommand,
            Optional<Func<RedisKey, IEnumerable<RedisKey>>> pushChanges,
            Guid guid
        )
        {
            if (input.Item1._tran != null)
            {
                input.Item1.Cmds.Enqueue
                (
                    (tran) => ValueTuple.Create 
                    (
                        ChannelChangedCommand(tran, KeyValuePair.Create(input.Item2.Key, serialize(input.Item2.Value))).ContinueWith(t => new KeyValuePair<Guid, object>(guid, (object)t.Result)),
                        pushChanges.HasValue ? pushChanges.Value(input.Item2.Key) : Enumerable.Empty<RedisKey>()
                    )
                );
            }
            return input;
        }

        public static ValueTuple<RedisCommandQueue, TInput> Enqueue<TInput, TOutput>
        (
            ValueTuple<RedisCommandQueue, TInput> input,
            Func<ITransaction, TInput, Task<TOutput>> cmd,
            Guid guid,
            Optional<Func<TInput, IEnumerable<string>>> keys
        )
        {
            if (input.Item1._tran != null)
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
            queue.ReceivedChangesBuilder.UnionWith(receivedChanges);
            return queue;
        }

        public static ImmutableHashSet<string> GetReceivedChanges(this RedisCommandQueue queue)
        {
            return queue.ReceivedChangesBuilder.ToImmutable();
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

                        if (queue._tran == null)
                        {
                            return;
                        }
                        foreach (var cmd in queue.Cmds)
                        {
                            var taskKey = cmd(queue._tran);
                            queue.Tasks.Enqueue(taskKey.Item1);
                            queue.ChangesBuilder.UnionWith(taskKey.Item2.Select(v => v.ToString()));
                        }
                        if (!queue.ChangesBuilder.IsEmpty())
                        {
                            var p = publishChanges.Invoke(queue.ChangesBuilder.ToImmutable());
                            queue.Tasks.Enqueue(queue._tran.PublishAsync(new RedisChannel(p.Item2 + "_" + queue._id.ToString(), p.Item3), p.Item1).ContinueWith(t => new KeyValuePair<Guid, object>(queue._id, (object)t.Result)));
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

                            if (queue._tran != null)
                            {
                                if (await queue._tran.ExecuteAsync())
                                {
                                    try
                                    {
                                        var resultAwaiter = Task.WhenAll(queue.Tasks).GetAwaiter();

                                        resultAwaiter.OnCompleted(() =>
                                        {

                                            //Pooled<ImmutableDictionary<Guid, object>.Builder> pooled = Pooled.GetDictionaryBuilder<Guid, object>();

                                            //foreach (var kv in resultAwaiter.GetResult())
                                            //{
                                            //    pooled.Value.TryAdd(kv.Key, kv.Value);

                                            //}
                                            //syncObs.OnNext(pooled.ToImmutableAndFree());


                                            ImmutableDictionary<Guid, object>.Builder builder = ImmutableDictionary.CreateBuilder<Guid, object>();
                                            foreach (var kv in resultAwaiter.GetResult())
                                            {
                                                builder.TryAdd(kv.Key, kv.Value);

                                            }
                                            syncObs.OnNext(builder.ToImmutable());


                                            action.Invoke((float)sw.ElapsedTicks / (float)(TimeSpan.TicksPerMillisecond));
                                            queue.Dispose();
                                        });
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex);
                                        syncObs.OnError(ex);
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("TransactionFailed");
                                    syncObs.OnError(new Exception("TransactionFailed"));
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
