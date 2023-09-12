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
using Stride.Core;
using VL.Lib.Reactive;
using System.Reactive.Subjects;
using System.Data.SqlTypes;

namespace VL.IO.Redis
{


    public static class RedisExtensions
    {
        public static Guid getID(this RedisCommandQueue queue) { return queue._id; }

        public static IObservable<RedisCommandQueue> RedisChangedCommand<TOutput>(
            IObservable<RedisBindingModel> model, 
            IObservable<Unit> enabled, 
            IObservable<RedisCommandQueue> queue,
            Func<ITransaction, RedisKey, Task<TOutput>> RedisChangedCommand,
            Guid getGuid)
        {
            bool firstFrame = true;

            return ReactiveExtensions
                .WithLatestWhenNew(enabled, queue, (f, s) => s)
                .WithLatestFrom(model,
                (queue, model) =>
                {
                    if (queue.Transaction != null)
                    {
                        if
                        (
                            (model.BindingType == RedisBindingType.Receive || model.BindingType == RedisBindingType.SendAndReceive) &&
                            (
                                (queue.ReceivedChanges.Contains(model.Key) && queue.ReceivedChanges.Count > 0) ||
                                (firstFrame && model.Initialisation == Initialisation.Redis)
                            )
                            ||
                            model.BindingType == RedisBindingType.AllwaysReceive
                        )
                        {
                            queue.Cmds.Enqueue
                            (
                                (tran) => ValueTuple.Create
                                (
                                    RedisChangedCommand(tran, model.Key).ContinueWith(t => new KeyValuePair<Guid, object>(getGuid, (object)t.Result)),
                                    Enumerable.Empty<RedisKey>()
                                )
                            );
                            firstFrame = false;
                        }
                    }
                    return queue;
                }
            );
        }

        public static IObservable<ValueTuple<RedisCommandQueue, KeyValuePair<RedisBindingModel, TInput>>> SerializeSetAndPushChanges<TInput, TInputSerialized, TOutput>(
            IObservable<KeyValuePair<RedisBindingModel, TInput>> channel,
            IObservable<RedisCommandQueue> queue,
            Func<TInput, TInputSerialized> serialize,
            Func<ITransaction, KeyValuePair<RedisKey, TInputSerialized>, Task<TOutput>> ChannelChangedCommand,
            Optional<Func<RedisKey, IEnumerable<RedisKey>>> pushChanges,
            Guid setGuid)
        {
            return ReactiveExtensions.
                WithLatestWhenNew(channel, queue, (c, q) =>
                {
                    return ValueTuple.Create(q, c);
                }).
                Select((input) =>
                {
                    var queue = input.Item1;
                    var model = input.Item2.Key;
                    var value = input.Item2.Value;

                    if (queue.Transaction != null)
                    {
                        if (model.BindingType == RedisBindingType.Send || model.BindingType == RedisBindingType.SendAndReceive)
                        {
                            queue.Cmds.Enqueue
                            (
                                (tran) => ValueTuple.Create
                                (
                                    ChannelChangedCommand(tran, KeyValuePair.Create(model.Key, serialize(value))).ContinueWith(t => new KeyValuePair<Guid, object>(setGuid, (object)t.Result)),
                                    pushChanges.HasValue ? pushChanges.Value(model.Key) : Enumerable.Empty<RedisKey>()
                                )
                            );
                        }
                    }
                    return input;
                }
            );
        }

        public static IObservable<ValueTuple<bool,bool>> Deserialize<TSetResult,TGetResult>(
            IChannel channel,
            IObservable<RedisBindingModel> model, 
            IObservable<ImmutableDictionary<Guid, object>> result, 
            Guid setGuid,
            Guid getGuid,
            Func<object, TSetResult> DeserializeSet,
            Func<object, TGetResult> DeserializeGet)
        {
            return result.WithLatestFrom(model)
                .Select(t => 
                {
                    var dict = t.Item1;
                    var model = t.Item2;

                    bool OnSuccessfulWrite = false;
                    bool OnSuccessfulRead  = false;

                    if (dict != null)
                    {
                        if (dict.TryGetValue(setGuid, out var setValue))
                        {
                            DeserializeSet(setValue);
                            OnSuccessfulWrite = true;
                        }
                        else
                        {
                            if (dict.TryGetValue(getGuid, out var getValue))
                            {
                                channel.SetObjectAndAuthor(DeserializeGet(getValue), "RedisOther");
                                OnSuccessfulRead = true;
                            }
                        }
                    }


                    return ValueTuple.Create(OnSuccessfulWrite, OnSuccessfulRead);
                });
        }



        /*
        public static RedisCommandQueue GetWhenChangeReceived<TOutput>
        (
            RedisCommandQueue queue,
            RedisBindingModel model,
            Func<ITransaction, RedisKey, Task<TOutput>> RedisChangedCommand,
            Guid guid,
            out bool ChangeReceived
        )
        {
            ChangeReceived = queue.ReceivedChanges.Contains(model.Key) && queue.ReceivedChanges.Count > 0 ;

            if (queue.Transaction != null && ChangeReceived) 
            {
                queue.Cmds.Enqueue
                (
                    (tran) => ValueTuple.Create
                    (
                        RedisChangedCommand(tran, model.Key).ContinueWith(t => new KeyValuePair<Guid, object>(guid, (object)t.Result)),
                        Enumerable.Empty<RedisKey>()
                    )
                );
            }
            return queue;
        }


        public static ValueTuple<RedisCommandQueue, KeyValuePair<RedisBindingModel, TInput>> SerializeSetAndPushChanges<TInput, TInputSerialized, TOutput>
        (
            ValueTuple<RedisCommandQueue, KeyValuePair<RedisBindingModel, TInput>> input,
            Func<TInput, TInputSerialized> serialize,
            Func<ITransaction, KeyValuePair<RedisKey, TInputSerialized>, Task<TOutput>> ChannelChangedCommand,
            Optional<Func<RedisKey, IEnumerable<RedisKey>>> pushChanges,
            Guid guid
        )
        {
            if (input.Item1.Transaction != null)
            {
                input.Item1.Cmds.Enqueue
                (
                    (tran) => ValueTuple.Create 
                    (
                        ChannelChangedCommand(tran, KeyValuePair.Create(input.Item2.Key.Key, serialize(input.Item2.Value))).ContinueWith(t => new KeyValuePair<Guid, object>(guid, (object)t.Result)),
                        pushChanges.HasValue ? pushChanges.Value(input.Item2.Key.Key) : Enumerable.Empty<RedisKey>()
                    )
                );
            }
            return input;
        }
        */

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

                        if (queue.Transaction == null)
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
                            queue.Tasks.Enqueue(queue.Transaction.PublishAsync(new RedisChannel(p.Item2 + "_" + queue._id.ToString(), p.Item3), p.Item1).ContinueWith(t => new KeyValuePair<Guid, object>(queue._id, (object)t.Result)));
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

                            if (queue.Transaction != null)
                            {
                                if (await queue.Transaction.ExecuteAsync())
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
