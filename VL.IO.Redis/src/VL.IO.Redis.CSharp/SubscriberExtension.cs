using StackExchange.Redis;
using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;


namespace VL.IO.Redis
{
    public static class SubscriberExtension
    {
        public static IObservable<TResult> Subscribe<TResult>(this ISubscriber subscriber, Func<RedisChannel, RedisValue, TResult> selector, string name, RedisChannel.PatternMode pattern = RedisChannel.PatternMode.Auto)
        {
            var channel = new RedisChannel(name, pattern);

            return Observable.Create<TResult>(async (obs, ct) =>
            {
                // as the SubscribeAsync callback can be invoked concurrently
                // a thread-safe wrapper for OnNext is needed
                var syncObs = Observer.Synchronize(obs);
                await subscriber.SubscribeAsync(channel, (chan, message) =>
                {
                    syncObs.OnNext(selector.Invoke(chan, message));
                }).ConfigureAwait(false);

                return Disposable.Create(() => subscriber.Unsubscribe(channel));
            });
        }

        public static IObservable<TResult> SubscribeScan<TSeed, TResult>(this ISubscriber subscriber, Func<TSeed, RedisChannel, RedisValue, TResult> selector, string name, RedisChannel.PatternMode pattern, TSeed seed)
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
