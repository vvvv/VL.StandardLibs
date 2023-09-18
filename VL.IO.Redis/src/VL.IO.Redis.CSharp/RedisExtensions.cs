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
using VL.Core.Reactive;
using System.Threading.Channels;

namespace VL.IO.Redis
{

    public static class RedisExtensions
    {
        public static Guid getID(this RedisCommandQueue queue) { return queue._id; }

        public static IDisposable Transaction<TInput, TInputSerialized, TGetResult, TSetResult>(
            IChannel<TInput> channel,
            Func<ITransaction, RedisKey, Task<TGetResult>> RedisChangedCommand,
            Func<ITransaction, KeyValuePair<RedisKey, TInputSerialized>, Task<TSetResult>> ChannelChangedCommand,
            Optional<Func<RedisKey, IEnumerable<RedisKey>>> pushChanges,
            Func<TInput, TInputSerialized> serialize,
            Func<TSetResult, bool> DeserializeSet,
            Func<TGetResult, TInput> DeserializeGet)
        {
            var onRedisChangeNotificationOrFirstFrame = OnRedisChangeNotificationOrFirstFrame(channel, RedisChangedCommand);

            var onChannelChange = SerializeSetAndPushChanges(channel, onRedisChangeNotificationOrFirstFrame, serialize, ChannelChangedCommand, pushChanges);

            var deserializeResult = Deserialize(channel, DeserializeSet, DeserializeGet);

            return onChannelChange.CombineLatest(deserializeResult, (c, d) => d).Subscribe();
        }

        internal static IObservable<RedisCommandQueue> OnRedisChangeNotificationOrFirstFrame<TInput, TGetResult>(
            IChannel<TInput> channel,
            Func<ITransaction, RedisKey, Task<TGetResult>> RedisChangedCommand)
        {
            bool firstFrame = true;

            return channel.Components.OfType<RedisBinding>().FirstOrDefault().AfterFrame
                .Select((queue) =>
                {
                    var model = channel.Components.OfType<RedisBinding>().FirstOrDefault();

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
                            model.BindingType == RedisBindingType.AlwaysReceive
                        )
                        {
                            queue.Cmds.Enqueue
                            (
                                (tran) => ValueTuple.Create
                                (
                                    RedisChangedCommand(tran, model.Key).ContinueWith(t => new KeyValuePair<Guid, object>(model.getID, (object)t.Result)),
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

        internal static IObservable<ValueTuple<RedisCommandQueue,  TInput>> SerializeSetAndPushChanges<TInput, TInputSerialized, TSetResult>(
            IChannel<TInput> channel,
            IObservable<RedisCommandQueue> queue,
            Func<TInput, TInputSerialized> serialize,
            Func<ITransaction, KeyValuePair<RedisKey, TInputSerialized>, Task<TSetResult>> ChannelChangedCommand,
            Optional<Func<RedisKey, IEnumerable<RedisKey>>> pushChanges)
        {
            return ReactiveExtensions.
                WithLatestWhenNew(channel, queue, (c, q) =>
                {
                    return ValueTuple.Create(q, c);
                }).
                Select((input) =>
                {
                    var queue = input.Item1;

                    var model = channel.Components.OfType<RedisBinding>().FirstOrDefault();
                    var value = input.Item2;

                    if (queue.Transaction != null && channel.LatestAuthor != "RedisOther")
                    {
                        if (model.BindingType == RedisBindingType.Send || model.BindingType == RedisBindingType.SendAndReceive)
                        {
                            queue.Cmds.Enqueue
                            (
                                (tran) => ValueTuple.Create
                                (
                                    ChannelChangedCommand(tran, KeyValuePair.Create(model.Key, serialize(value))).ContinueWith(t => new KeyValuePair<Guid, object>(model.setID, (object)t.Result)),
                                    pushChanges.HasValue ? pushChanges.Value(model.Key) : Enumerable.Empty<RedisKey>()
                                )
                            );
                        }
                    }
                    return input;
                }
            );
        }

        internal static IObservable<ValueTuple<bool,bool,bool>> Deserialize<TInput, TSetResult, TGetResult>(
            IChannel<TInput> channel,
            Func<TSetResult, bool> DeserializeSet,
            Func<TGetResult, TInput> DeserializeGet)
        {
            var beforeFrameResult = channel.Components.OfType<RedisBinding>().FirstOrDefault().BeforFrame
                .Select(t => 
                {
                    var dict = t;
                    var model = channel.Components.OfType<RedisBinding>().FirstOrDefault();

                    bool OnSuccessfulWrite = false;
                    bool OnSuccessfulRead  = false;
                    TInput result = default(TInput);

                    if (dict != null)
                    {
                        if (model.CollisionHandling == CollisionHandling.RedisWins)
                        {
                            if (dict.TryGetValue(model.getID, out var getValue))
                            {
                                result = DeserializeGet((TGetResult)getValue);
                                channel.SetObjectAndAuthor(result, "RedisOther");
                                OnSuccessfulRead = true;
                            }
                            else
                            {
                                if (dict.TryGetValue(model.setID, out var setValue))
                                {
                                    OnSuccessfulWrite = DeserializeSet((TSetResult)setValue);
                                }
                                
                            }
                        }
                        else
                        {
                            if (dict.TryGetValue(model.setID, out var setValue))
                            {
                                OnSuccessfulWrite = DeserializeSet((TSetResult)setValue);
                            }
                            else
                            {
                                if (dict.TryGetValue(model.getID, out var getValue))
                                {
                                    result = DeserializeGet((TGetResult)getValue);
                                    channel.SetObjectAndAuthor(result, "RedisOther");
                                    OnSuccessfulRead = true;
                                }
                            }
                        }
                    }


                    return ValueTuple.Create(OnSuccessfulWrite, OnSuccessfulRead, false, result);
                })
                .Publish()
                .RefCount();

            var nextFrameChannelUpdates = channel
                .Where(
                (value) => 
                    channel.LatestAuthor != "RedisOther" && 
                    channel.Components.OfType<RedisBinding>().FirstOrDefault().CollisionHandling == CollisionHandling.RedisWins)
                .WithLatestFrom(beforeFrameResult, 
                (value,resultTuple) => 
                {
                    bool OnSuccessfulRead = false;
                    bool OnRedisOverWrite = false;
                    
                            if (resultTuple.Item2)
                            {
                                channel.SetObjectAndAuthor(resultTuple.Item4, "RedisOther");
                                OnSuccessfulRead = true;
                                OnRedisOverWrite = true;
                            }

                    return ValueTuple.Create(false, OnSuccessfulRead, OnRedisOverWrite, default(TInput));
                })
                .Where(T => T.Item3);

            return beforeFrameResult
                    .Merge(nextFrameChannelUpdates)
                    .Select(t =>
                    {
                        var result = channel.Components.OfType<RedisResult>().FirstOrDefault();
                        result.OnSuccessfulWrite = t.Item1;
                        result.OnSuccessfulRead = t.Item2;
                        result.OnRedisOverWrite = t.Item3;
                        return ValueTuple.Create(t.Item1, t.Item2, t.Item3);
                    });
        }

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
