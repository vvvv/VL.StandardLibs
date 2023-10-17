using StackExchange.Redis;
using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using VL.Core;

namespace VL.IO.Redis
{
    public static class SubscriberExtension
    {
        public static IObservable<Int64> Publish<T>(this ConnectionMultiplexer connectionMultiplexer, IObservable<T> value, Func<T,RedisValue> serialize, string RedisChannel, RedisChannel.PatternMode pattern = RedisChannel.PatternMode.Auto)
        {
            var redisChannel = new RedisChannel(RedisChannel, pattern);

            return Observable.Create<Int64>(async (obs, ct) =>
            {
                // as the SubscribeAsync callback can be invoked concurrently
                // a thread-safe wrapper for OnNext is needed
                var syncObs = Observer.Synchronize(obs);
                var subscriber = connectionMultiplexer.GetSubscriber();

                value.Subscribe(
                    // onNext
                    async value => 
                    {
                        if (connectionMultiplexer.IsConnected)
                        {
                            syncObs.OnNext(await subscriber.PublishAsync(redisChannel, serialize(value)));
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
                    });


                // Return Disposable
                return Disposable.Create(() => subscriber.Unsubscribe(redisChannel));
            });

        }

        private static async void subscribe<TResult>(IVLRuntime? vLRuntime, NodeContext nodeContext, CompositeDisposable warnings, IObserver<TResult> syncObs, ISubscriber subscriber, RedisChannel channel, Func<RedisChannel, RedisValue, TResult> deserialize)
        {
            
            await subscriber.SubscribeAsync(channel, (chan, message) =>
            {
                if (!message.IsNullOrEmpty)
                {
                    try
                    {
                        syncObs.OnNext(deserialize.Invoke(chan, message));
                        if (!warnings.IsDisposed)
                            warnings.Dispose();
                        foreach (var id in nodeContext.Path.Stack)
                        {
                            vLRuntime.AddMessage(new VL.Lang.Message(id, Lang.MessageSeverity.Warning, "Subscribed message fail to Deserialize."));
                        }
                    }
                    catch (Exception ex)
                    {
                        foreach (var id in nodeContext.Path.Stack)
                        {
                            vLRuntime.AddPersistentMessage(new VL.Lang.Message(id, Lang.MessageSeverity.Error, "Subscribed message fail to Deserialize." + Environment.NewLine + ex.Message)).DisposeBy(warnings);
                        }
                    }
                }
            }).ConfigureAwait(false);
        }

        private static async void subscribeScan<TSeed, TResult>(IVLRuntime? vLRuntime, NodeContext nodeContext, CompositeDisposable warnings, IObserver<TResult> syncObs, ISubscriber subscriber, RedisChannel channel, Func<TSeed, RedisChannel, RedisValue, TResult> selector, TSeed seed)
        {
            await subscriber.SubscribeAsync(channel, (chan, message) =>
            {
                if (!message.IsNullOrEmpty)
                {
                    try
                    {
                        syncObs.OnNext(selector.Invoke(seed, chan, message));
                        if (!warnings.IsDisposed)
                            warnings.Dispose();
                    }
                    catch (Exception ex)
                    {
                        foreach (var id in nodeContext.Path.Stack)
                        {
                            vLRuntime.AddPersistentMessage(new VL.Lang.Message(id, Lang.MessageSeverity.Error, "Subscribed message fail to Deserialize." + Environment.NewLine + ex.Message)).DisposeBy(warnings);
                        }
                    }
                }
                foreach (var id in nodeContext.Path.Stack)
                {
                    vLRuntime.AddMessage(new VL.Lang.Message(id, Lang.MessageSeverity.Error, "Subscribed message fail to Deserialize."));
                }
            }).ConfigureAwait(false);
        }

        public static void Warning(NodeContext nodeContext)
        {
            foreach (var id in nodeContext.Path.Stack)
            {
                IVLRuntime.Current?.AddMessage(new VL.Lang.Message(id, Lang.MessageSeverity.Error, "Subscribed message fail to Deserialize."));
            }
        }

        public static IObservable<TResult> Subscribe<TResult>(this ConnectionMultiplexer connectionMultiplexer, NodeContext nodeContext, Func<RedisChannel, RedisValue, TResult> deserialize, string RedisChannel, RedisChannel.PatternMode pattern = RedisChannel.PatternMode.Auto)
        {
            var channel = new RedisChannel(RedisChannel, pattern);
            var warnings = new CompositeDisposable();

            return Observable.Create<TResult>(async (obs, ct) =>
            {
                // as the SubscribeAsync callback can be invoked concurrently
                // a thread-safe wrapper for OnNext is needed
                var syncObs = Observer.Synchronize(obs);
                var subscriber = connectionMultiplexer.GetSubscriber();

                // Handle Connection Failed
                connectionMultiplexer.ConnectionFailed += async (s, e) => 
                {
                    //await subscriber.UnsubscribeAsync(channel).ConfigureAwait(false);
                };

                // Handle Connection Restored
                connectionMultiplexer.ConnectionRestored += async (s, e) =>
                {
                    subscribe(IVLRuntime.Current, nodeContext, warnings, syncObs, subscriber, channel, deserialize);
                };

                // Subscribe
                subscribe(IVLRuntime.Current, nodeContext, warnings, syncObs, subscriber, channel, deserialize);

                // Return Disposable
                return Disposable.Create(() =>
                {
                    if (!warnings.IsDisposed)
                        warnings.Dispose();
                    subscriber.Unsubscribe(channel);
                });
            });
        }

        public static IObservable<TResult> SubscribeScan<TSeed, TResult>(this ConnectionMultiplexer connectionMultiplexer, NodeContext nodeContext, Func<TSeed, RedisChannel, RedisValue, TResult> selector, string RedisChannel, RedisChannel.PatternMode pattern, TSeed seed)
        {
            var channel = new RedisChannel(RedisChannel, pattern);
            var warnings = new CompositeDisposable();

            return Observable.Create<TResult>(async (obs, ct) =>
            {
                // as the SubscribeAsync callback can be invoked concurrently
                // a thread-safe wrapper for OnNext is needed
                var syncObs = Observer.Synchronize(obs);

                var subscriber = connectionMultiplexer.GetSubscriber();

                // Handle Connection Failed
                connectionMultiplexer.ConnectionFailed += async (s, e) =>
                {
                    //await subscriber.UnsubscribeAsync(channel).ConfigureAwait(false);
                };

                // Handle Connection Restored
                connectionMultiplexer.ConnectionRestored += async (s, e) =>
                {
                    subscribeScan(IVLRuntime.Current, nodeContext, warnings, syncObs, subscriber, channel, selector, seed);
                };

                // Subscribe
                subscribeScan(IVLRuntime.Current, nodeContext, warnings, syncObs, subscriber, channel, selector, seed);

                // Return Disposable
                return Disposable.Create(() =>
                {
                    if (!warnings.IsDisposed)
                        warnings.Dispose();
                    subscriber.Unsubscribe(channel);
                });
            });
        }
    }
}
