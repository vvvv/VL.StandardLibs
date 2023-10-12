using StackExchange.Redis;
using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

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

        private static async void subscribe<TResult>(IObserver<TResult> syncObs, ISubscriber subscriber, RedisChannel channel, Func<RedisChannel, RedisValue, TResult> deserialize)
        {
            await subscriber.SubscribeAsync(channel, (chan, message) =>
            {
                if (!message.IsNullOrEmpty)
                {
                    try
                    {
                        syncObs.OnNext(deserialize.Invoke(chan, message));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("### MESSAGE FAIL TO DESERIALIZE ###");
                        Console.WriteLine(message.ToString());
                        Console.WriteLine("###################################");

                    }
                }
            }).ConfigureAwait(false);
        }

        private static async void subscribeScan<TSeed, TResult>(IObserver<TResult> syncObs, ISubscriber subscriber, RedisChannel channel, Func<TSeed, RedisChannel, RedisValue, TResult> selector, TSeed seed)
        {
            await subscriber.SubscribeAsync(channel, (chan, message) =>
            {
                if (!message.IsNullOrEmpty)
                {
                    try
                    {
                        syncObs.OnNext(selector.Invoke(seed, chan, message));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("### MESSAGE FAIL TO DESERIALIZE ###");
                        Console.WriteLine(message.ToString());
                        Console.WriteLine("###################################");
                    }
                }
            }).ConfigureAwait(false);
        }

        public static IObservable<TResult> Subscribe<TResult>(this ConnectionMultiplexer connectionMultiplexer, Func<RedisChannel, RedisValue, TResult> deserialize, string RedisChannel, RedisChannel.PatternMode pattern = RedisChannel.PatternMode.Auto)
        {
            var channel = new RedisChannel(RedisChannel, pattern);

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
                    subscribe(syncObs, subscriber, channel, deserialize);
                };

                // Subscribe
                subscribe(syncObs, subscriber, channel, deserialize);

                // Return Disposable
                return Disposable.Create(() => subscriber.Unsubscribe(channel));
            });
        }

        public static IObservable<TResult> SubscribeScan<TSeed, TResult>(this ConnectionMultiplexer connectionMultiplexer, Func<TSeed, RedisChannel, RedisValue, TResult> selector, string RedisChannel, RedisChannel.PatternMode pattern, TSeed seed)
        {
            var channel = new RedisChannel(RedisChannel, pattern);

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
                    subscribeScan(syncObs, subscriber, channel, selector, seed);
                };

                // Subscribe
                subscribeScan(syncObs, subscriber, channel, selector, seed);

                // Return Disposable
                return Disposable.Create(() => subscriber.Unsubscribe(channel));
            });
        }
    }
}
