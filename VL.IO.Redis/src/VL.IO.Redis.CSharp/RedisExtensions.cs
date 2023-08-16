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
using System.Diagnostics;
using VL.Core;
using System.Security.Cryptography;
using VL.Lib.Collections;
using System.Reflection.Metadata.Ecma335;

namespace VL.IO.Redis
{
    
    public sealed class ThreadSafeToggle
    {
        public ThreadSafeToggle() { }

        private bool enabled = true;
        private object syncObj = new object();

        public void Enable()
        {
            lock (syncObj)
            {
                enabled = true;
            }
        }
        public void Disable()
        {
            lock (syncObj)
            {
                enabled = false;
            }
        }
        public bool Enabled()
        {
            lock (syncObj)
            {
                return enabled;
            }
        }
    }

    public class Transaction
    {

    }

    public static class RedisExtensions
    {
        /// <summary>
        /// xs --x---x---x---x-------x---x---x-
        ///       \           \       \        
        /// ys ----y-----------y---y---y-------
        ///        |           |       |       
        /// rs ----x-----------x-------x-------
        ///        y           y       y       
        /// </summary>
        /// <typeparam name="TLeft"></typeparam>
        /// <typeparam name="TRight"></typeparam>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static IObservable<ValueTuple<TRight,TLeft>> WithLatestWhenNew<TLeft, TRight>(this IObservable<TLeft> left, IObservable<TRight> right)
        {
            return left.Join(
                right,
                l => left.Any().Merge(right.Any()),
                r => Observable.Empty<Unit>(),
                (l, r) => { return ValueTuple.Create(r, l); });

        }

        public static ValueTuple<RedisCommandQueue, TInput> Enqueue<TInput,TOutput>(ValueTuple<RedisCommandQueue,TInput> input, Func<ITransaction, TInput, Task<TOutput>> cmd, Guid guid, Optional<Func<TInput,IEnumerable<string>>> keys)
        {
            input.Item1._cmds.Enqueue((tran) => cmd.Invoke(tran, input.Item2).ContinueWith(t => new KeyValuePair<Guid, object>(guid, (object)t.Result)));

            if (keys.HasValue)
            {
                input.Item1._changes.AddRange(keys.Value.Invoke(input.Item2));
     
            }

            return input;
        }

        public static IObservable<RedisCommandQueue> ApplyTransactions(this IObservable<RedisCommandQueue> observable, Action<float, int> action, Func<Spread<string>,RedisValue> serializeChanges, Guid guid)
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
                            queue._tasks.Add(queue._tran.PublishAsync("Changed", serializeChanges.Invoke(queue._changes.ToSpread()) ).ContinueWith(t => new KeyValuePair<Guid, object>(guid, (object)t.Result)));

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
                                    action.Invoke((float)sw.ElapsedTicks / (float)(TimeSpan.TicksPerMillisecond));

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
