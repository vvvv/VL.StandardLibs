using Newtonsoft.Json.Linq;
using ServiceWire;
using StackExchange.Redis;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
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

            return Observable.Create<Int64>((obs, ct) =>
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
                            RedisValue result = RedisValue.Null;
                            try
                            {
                                // Try serialize
                                result = serialize.Invoke(value);

                                // If serialize don't throw an exeption
                                syncObs.OnNext(await subscriber.PublishAsync(redisChannel, result));

                                if (warnings.Count > 0) warnings.Clear();
                            }
                            catch (RedisConnectionException ex)
                            {
                                warnings.AddExeption("Published message fail to Send.", ex, nodeContext, runtime);
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
                return Task.FromResult(Disposable.Create(() =>
                {
                    disposable.Dispose();

                    if (!warnings.IsDisposed)
                        warnings.Dispose();
                }));
            });

        }

        public static IObservable<TResult> Subscribe<TResult>(this ConnectionMultiplexer connectionMultiplexer, NodeContext nodeContext, Func<RedisChannel, RedisValue, TResult> deserialize, string RedisChannel, RedisChannel.PatternMode pattern = RedisChannel.PatternMode.Auto)
        {
            var channel = new RedisChannel(RedisChannel, pattern);
            var warnings = new CompositeDisposable();
            var runtime = IVLRuntime.Current;
            ChannelMessageQueue messages = null;

            return Observable.Create<TResult>(async (obs, ct) =>
            {
                // as the SubscribeAsync callback can be invoked concurrently
                // a thread-safe wrapper for OnNext is needed
                var syncObs = Observer.Synchronize(obs);               

                // Handle Connection Restored
                connectionMultiplexer.ConnectionRestored += async (s, e) =>
                {
                    // ReSubscribe
                    messages = await connectionMultiplexer.GetSubscriber().SubscribeAsync(channel);
                };

                // Subscribe
                messages = await connectionMultiplexer.GetSubscriber().SubscribeAsync(channel);

                // OnMesssage
                messages.OnMessage(channelMessage =>
                {
                    if (!channelMessage.Message.IsNullOrEmpty)
                    {
                        try
                        {
                            // Try deserialize
                            TResult result = deserialize.Invoke(channel, channelMessage.Message);

                            // If deserialize don't throw an exeption
                            syncObs.OnNext(result);

                            if (warnings.Count > 0) warnings.Clear();
                        }
                        catch (Exception ex)
                        {
                            warnings.AddExeption("Subscribed message fail to Deserialize.", ex, nodeContext, runtime);
                        }
                    }
                });

                // Return Disposable
                return Disposable.Create(async () =>
                {
                    if (!warnings.IsDisposed)
                        warnings.Dispose();
                    await messages.UnsubscribeAsync();
                });
            });
        }

        public static IObservable<TResult> SubscribeScan<TSeed, TResult>(this ConnectionMultiplexer connectionMultiplexer, NodeContext nodeContext, Func<TSeed, RedisChannel, RedisValue, TResult> selector, string RedisChannel, RedisChannel.PatternMode pattern, TSeed seed)
        {
            var channel = new RedisChannel(RedisChannel, pattern);
            var warnings = new CompositeDisposable();
            var runtime = IVLRuntime.Current;
            ChannelMessageQueue messages = null;

            return Observable.Create<TResult>(async (obs, ct) =>
            {
                // as the SubscribeAsync callback can be invoked concurrently
                // a thread-safe wrapper for OnNext is needed
                var syncObs = Observer.Synchronize(obs);

                // Handle Connection Restored
                connectionMultiplexer.ConnectionRestored += async (s, e) =>
                {
                    // ReSubscribe
                    messages = await connectionMultiplexer.GetSubscriber().SubscribeAsync(channel);
                };

                // Subscribe
                messages = await connectionMultiplexer.GetSubscriber().SubscribeAsync(channel);

                // OnMesssage
                messages.OnMessage(channelMessage =>
                {
                    if (!channelMessage.Message.IsNullOrEmpty)
                    {
                        try
                        {
                            // Try deserialize
                            TResult result = selector.Invoke(seed, channel, channelMessage.Message);

                            // If deserialize don't throw an exeption
                            syncObs.OnNext(result);

                            if (warnings.Count > 0) warnings.Clear();
                        }
                        catch (Exception ex)
                        {
                            warnings.AddExeption("Subscribed message fail to Deserialize.", ex, nodeContext, runtime);
                        }
                    }
                });

                // Return Disposable
                return Disposable.Create(() =>
                {
                    if (!warnings.IsDisposed)
                        warnings.Dispose();
                    messages.Unsubscribe();
                });
            });
        }
    }
}
