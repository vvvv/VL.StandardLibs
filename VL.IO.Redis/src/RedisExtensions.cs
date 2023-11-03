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
using System.Reactive.Disposables;

namespace VL.IO.Redis
{

    public static class RedisExtensions
    {
        internal static ConnectionMultiplexer EnableClientSideCaching(this ConnectionMultiplexer ConnectionMultiplexer, string ClientName, out Spread<long> ClientID, out Spread<bool> IsEnabled)
        {
            SpreadBuilder<long> ClientIDBuilder = new SpreadBuilder<long>();
            SpreadBuilder<bool> IsEnabledBuilder = new SpreadBuilder<bool>();

            foreach (var server in ConnectionMultiplexer.GetServers())
            {
                var info = server.ClientList().FirstOrDefault(
                    (ClientInfo info) =>
                    {
                        return info.Name == ClientName && info.SubscriptionCount > 0;
                    }
                );
                if (info != null) 
                {
                    ClientIDBuilder.Add(info.Id);
                    try
                    {
                        server.Execute("CLIENT", new object[] { "TRACKING", "ON", "REDIRECT", info.Id.ToString(), "BCAST", "NOLOOP" });
                        IsEnabledBuilder.Add(true);
                    }
                    catch 
                    {
                        IsEnabledBuilder.Add(false);
                    }
                    
                }
            }

            ClientID = ClientIDBuilder.ToSpread();
            IsEnabled = IsEnabledBuilder.ToSpread();
            return ConnectionMultiplexer;
        }

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
                    (tran) => cmd.Invoke(tran, input.Item2).ContinueWith(t => new KeyValuePair<Guid, object>(guid, (object)t.Result))
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

        public static IObservable<RedisCommandQueue> ApplyTransactions(this IObservable<RedisCommandQueue> observable, Action<float, int> action, Func<ImmutableHashSet<string>,Tuple<RedisValue,string,RedisChannel.PatternMode,bool>> publishChanges, NodeContext nodeContext)
        {
            CompositeDisposable warnings = new CompositeDisposable();
            IVLRuntime runtime = IVLRuntime.Current;

            return Observable.Create<RedisCommandQueue>((obs) =>
            {
                var syncObs = Observer.Synchronize(obs, true);
                var disposable = observable.Subscribe(
                    // onNext
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
                                queue.Tasks.Enqueue(taskKey);
                            }
                            action.Invoke((float)sw.ElapsedTicks / (float)(TimeSpan.TicksPerMillisecond), queue.Tasks.Count);

                            syncObs.OnNext(queue);
                        
                            if(warnings.Count > 0) warnings.Clear();
                        }
                        catch (Exception ex)
                        {
                            warnings.AddExeption("ApplyTransactions failed.", ex, nodeContext, runtime);
                        }
                    }
                    // onError
                    , (ex) =>
                    {
                        warnings.Clear();
                        warnings.AddExeption("Observable throw an Exeption.", ex, nodeContext, runtime);
                    }
                    // onComplete
                    , () =>
                    {
                        warnings.Clear();
                        warnings.AddExeption("Observable completed.", new Exception(), nodeContext, runtime);
                    }
                );
                return Disposable.Create(() =>
                {
                    disposable.Dispose();
                    if (!warnings.IsDisposed)
                        warnings.Dispose();
                });
            });
        }


        public static IObservable<ImmutableDictionary<Guid, object>> ExecuteTransaction(this IObservable<RedisCommandQueue> observable, Action<float> action, IScheduler scheduler, NodeContext nodeContext)
        {

            ImmutableDictionary<Guid, object>.Builder builder = ImmutableDictionary.CreateBuilder<Guid, object>();
            CompositeDisposable warnings = new CompositeDisposable();
            IVLRuntime runtime = IVLRuntime.Current;

            return Observable.Create<ImmutableDictionary<Guid, object>>(
                (obs) =>
                {
                    //var syncObs = Observer.Synchronize(obs, true);
                    var syncObs = Observer.NotifyOn(obs, scheduler);

                    var disposable = observable.Subscribe(async
                        // onNext
                        (queue) =>
                        {
                            var sw = Stopwatch.StartNew();

                            if (queue.Transaction != null && queue.Multiplexer.IsConnected)
                            {
                                try
                                {
                                    if (await queue.Transaction.ExecuteAsync())
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
                                  
                                    if (warnings.Count > 0) warnings.Clear();
                                }
                                catch (Exception ex)
                                {
                                    warnings.AddExeption("Execute Transaction failed.", ex, nodeContext, runtime);
                                }
                               
                            }
                        }
                        // onError
                        , (ex) =>
                        {
                            warnings.Clear();
                            warnings.AddExeption("Observable throw an Exeption.", ex, nodeContext, runtime);
                        }
                        // onComplete
                        , () =>
                        {
                            warnings.Clear();
                            warnings.AddExeption("Observable completed.", new Exception(), nodeContext, runtime);
                        }
                    );
                    return Disposable.Create(() =>
                    {
                        disposable.Dispose();
                        if (!warnings.IsDisposed)
                            warnings.Dispose();
                    });
                }
            );
        }
    }
}
