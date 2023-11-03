using Newtonsoft.Json.Linq;
using ServiceWire;
using StackExchange.Redis;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Channels;
using VL.Core;

namespace VL.IO.Redis
{
    public static class SubscriberExtension
    {

        public static IObservable<Int64> Publish<T>(this ConnectionMultiplexer connectionMultiplexer, NodeContext nodeContext, IObservable<T> value, Func<T,RedisValue> serialize, string RedisChannel, RedisChannel.PatternMode pattern = RedisChannel.PatternMode.Auto)
        {
            var redisChannel = new RedisChannel(RedisChannel, pattern);

            var warnings = new CompositeDisposable();
            IVLRuntime runtime = IVLRuntime.Current;

            return Observable.Create<Int64>(async (obs, ct) =>
            {
                // as the SubscribeAsync callback can be invoked concurrently
                // a thread-safe wrapper for OnNext is needed
                var syncObs = Observer.Synchronize(obs);
                var subscriber = connectionMultiplexer.GetSubscriber();

                var disposable = value.Subscribe(
                    // onNext
                    async value => 
                    {
                        if (connectionMultiplexer.IsConnected)
                        {
                            try
                            {
                                // Try serialize
                                RedisValue result = serialize.Invoke(value);

                                // If serialize don't throw an exeption
                                syncObs.OnNext(await subscriber.PublishAsync(redisChannel, result));

                                if (warnings.Count > 0) warnings.Clear();
                            }
                            catch (Exception ex)
                            {
                                warnings.AddExeption("Published message fail to Serialize.", ex, nodeContext, runtime);
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


                // Return Disposable
                return Disposable.Create(() =>
                {
                    disposable.Dispose();

                    if (!warnings.IsDisposed)
                        warnings.Dispose();
                });
            });

        }

        private static async void subscribe<TResult>(NodeContext nodeContext, CompositeDisposable warnings, IObserver<TResult> syncObs, ISubscriber subscriber, RedisChannel channel, Func<RedisChannel, RedisValue, TResult> deserialize)
        {
            IVLRuntime runtime = IVLRuntime.Current;

            await subscriber.SubscribeAsync(channel, (chan, message) =>
            {
                if (!message.IsNullOrEmpty)
                {
                    try
                    {
                        // Try deserialize
                        TResult result = deserialize.Invoke(chan, message);

                        // If deserialize don't throw an exeption
                        syncObs.OnNext(result);

                        if (warnings.Count > 0) warnings.Clear();
                    }
                    catch (Exception ex)
                    {
                        
                        warnings.AddExeption("Subscribed message fail to Deserialize.", ex, nodeContext, runtime);
                    }
                }
            }).ConfigureAwait(false);
        }

        private static async void subscribeScan<TSeed, TResult>(NodeContext nodeContext, CompositeDisposable warnings, IObserver<TResult> syncObs, ISubscriber subscriber, RedisChannel channel, Func<TSeed, RedisChannel, RedisValue, TResult> selector, TSeed seed)
        {
            IVLRuntime runtime = IVLRuntime.Current;

            await subscriber.SubscribeAsync(channel, (chan, message) =>
            {
                if (!message.IsNullOrEmpty)
                {
                    try
                    {
                        // Try deserialize
                        TResult result = selector.Invoke(seed, chan, message);

                        // If deserialize don't throw an exeption
                        syncObs.OnNext(result);

                        if (warnings.Count > 0) warnings.Clear();
                    }
                    catch (Exception ex)
                    {
                        warnings.AddExeption("Subscribed message fail to Deserialize.", ex, nodeContext, runtime);
                    }
                }
            }).ConfigureAwait(false);
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
                    subscribe(nodeContext, warnings, syncObs, subscriber, channel, deserialize);
                };

                // Subscribe
                subscribe(nodeContext, warnings, syncObs, subscriber, channel, deserialize);

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
                    subscribeScan(nodeContext, warnings, syncObs, subscriber, channel, selector, seed);
                };

                // Subscribe
                subscribeScan(nodeContext, warnings, syncObs, subscriber, channel, selector, seed);

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
