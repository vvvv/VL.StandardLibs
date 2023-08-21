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
using System.Reactive.Joins;
using System.Xml.Linq;
using ServiceWire;

namespace VL.IO.Redis
{
    public static class RedisExtensions
    {
        /// <summary>
        /// first  --x---x---x---x-------x---x---x-
        ///           \           \       \        
        /// second ----y-----------y---y---y-------
        ///            |           |       |       
        /// result ----x-----------x-------x-------
        ///            y           y       y       
        ///            
        /// http://introtorx.com/Content/v1.0.10621.0/17_SequencesOfCoincidence.html#Join
        /// https://stackoverflow.com/questions/13319241/combine-two-observables-but-only-when-the-first-obs-is-immediately-preceded-by?rq=3
        /// </summary>
        /// <typeparam name="TFirst"></typeparam>
        /// <typeparam name="TSecond"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static IObservable<TResult> WithLatestWhenNew<TFirst, TSecond, TResult>(IObservable<TFirst> first, IObservable<TSecond> second, Func<TFirst,TSecond,TResult> selector)
        {

            var left = first.Publish().RefCount();
            var rigth = second.Publish().RefCount();

            return Observable.Join(
                left,
                rigth,
                // leftDurationSellector
                _ => left.Any().Merge(rigth.Any()),
                // rightDurationSellector
                _ => Observable.Empty<Unit>(),
                // resultSelector
                (l, r) => { return selector.Invoke(l, r); }
            
                );
        }

        public static IObservable<TResult> SelectOrWithLatestFrom<TFirst, TSecond, TResult>(IObservable<TFirst> first, IObservable<TSecond> second, Func<TFirst, TResult> select, Func<TResult, TSecond, TResult> WithLatestFromSecondWhenFirst)
        {
            var secondRef = second.Publish().RefCount();
            var firstTransformedRef = first.Select(select).Publish().RefCount();

            return Observable.Join(
                secondRef,
                firstTransformedRef,
                // leftDurationSellector
                _ => secondRef.Any().Merge(firstTransformedRef.Any()),
                // rightDurationSellector
                _ => Observable.Empty<Unit>(),
                // resultSelector
                (l, r) => { return WithLatestFromSecondWhenFirst(r, l); }
                )
                .Merge(firstTransformedRef)
                .Buffer(firstTransformedRef)
                .Select(l => l.FirstOrDefault())
                .Publish()
                .RefCount();
        }

        public static IObservable<C> MergeOrCombineLatest<A, B, C>(
            IObservable<A> a,
            IObservable<B> b,
            Func<A, C> aResultSelector, // When A starts before B
            Func<B, C> bResultSelector, // When B starts before A
            Func<A, B, C> bothResultSelector) // When both A and B have started
        {
            return
                a.Publish(aa =>
                    b.Publish(bb =>
                        aa.CombineLatest(bb, bothResultSelector).Publish(xs =>
                            aa
                                .Select(aResultSelector)
                                .Merge(bb.Select(bResultSelector))
                                .TakeUntil(xs)
                                .SkipLast(1)
                                .Merge(xs))));
        }

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

        public static IObservable<RedisCommandQueue> ApplyTransactions(this IObservable<RedisCommandQueue> observable, Action<float, int> action, Func<Spread<string>,Tuple<RedisValue,string,RedisChannel.PatternMode,bool>> publishChanges, Guid guid)
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
                                    queue._tasks.Add(queue._tran.PublishAsync(new RedisChannel(p.Item2 + "_" + guid.ToString(), p.Item3), p.Item1).ContinueWith(t => new KeyValuePair<Guid, object>(guid, (object)t.Result)));
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

        public static Task<KeyValuePair<Guid, object>> Cast<T>(this Task<T> task, Guid guid)
        {
            return task.ContinueWith(t => new KeyValuePair<Guid,object>(guid, (object)t.Result));
        }

        public static IObservable<TResult> Subscribe<TResult>(this ISubscriber subscriber, Func<RedisChannel,RedisValue,TResult> selector, string name, RedisChannel.PatternMode pattern = RedisChannel.PatternMode.Auto)
        {
            var channel = new RedisChannel(name, pattern);

            return Observable.Create<TResult>(async (obs, ct) =>
            {
                // as the SubscribeAsync callback can be invoked concurrently
                // a thread-safe wrapper for OnNext is needed
                var syncObs = Observer.Synchronize(obs);
                await subscriber.SubscribeAsync(channel, (chan, message) =>
                {
                    syncObs.OnNext(selector.Invoke(chan,message));
                }).ConfigureAwait(false);

                return Disposable.Create(() => subscriber.Unsubscribe(channel));
            });
        }

        public static IObservable<TResult> SubscribeScan<TSeed,TResult>(this ISubscriber subscriber, Func<TSeed, RedisChannel, RedisValue, TResult> selector, string name, RedisChannel.PatternMode pattern, TSeed seed)
        {
            var channel = new RedisChannel(name, pattern);

            return Observable.Create<TResult>(async (obs, ct) =>
            {
                // as the SubscribeAsync callback can be invoked concurrently
                // a thread-safe wrapper for OnNext is needed
                var syncObs = Observer.Synchronize(obs);
                await subscriber.SubscribeAsync(channel, (chan, message) =>
                {
                    syncObs.OnNext(selector.Invoke(seed, chan, message));
                }).ConfigureAwait(false);

                return Disposable.Create(() => subscriber.Unsubscribe(channel));
            });
        }
    }

   
}
