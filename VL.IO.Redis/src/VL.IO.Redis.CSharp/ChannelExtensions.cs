using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using VL.Lib.Reactive;

namespace VL.IO.Redis
{

    public static class ChannelExtensions
    {
        public static IChannel<T> EnsureSingleKey<T>(this IChannel<T> channel, RedisKey key)
        {
            var keys = channel.Components.OfType<RedisKey>();

            if (keys.Any())
            {
                if (keys.Count() == 1)
                {
                    if (keys.FirstOrDefault() ==  key)
                    {
                        return channel;
                    }
                    else
                    {
                        channel.Components = channel.Components.Replace(keys.First(), key);
                        return channel;
                    }
                }
                else
                {
                    var builder = channel.Components.ToBuilder();
                    
                    foreach (var k in keys)
                    {
                        builder.Remove(k);
                    }
                    builder.Add(key);
                    channel.Components = builder.ToImmutable();
                    return channel;
                }
            }
            else 
            {
                channel.Components = channel.Components.Add(key);
                return channel;
            }       
        }

        public static IChannel<T> TryGetKey<T>(this IChannel<T> channel, out bool success, out RedisKey key)
        {
            key = channel.Components.OfType<RedisKey>().FirstOrDefault();
            success = key != "(null)";
            return channel;
        }

        public static IObservable<KeyValuePair<RedisKey, T>> ToKeyValueObservable<T>(this IChannel<T> channel, out IObservable<RedisKey> key)
        {

            key = Observable.Start(
                () => 
                { 
                    return channel.Components.OfType<RedisKey>().FirstOrDefault(); 
                }
            );

            return Observable.Create<KeyValuePair<RedisKey, T>>((obs) =>
            {
                var syncObs = Observer.Synchronize(obs);
                return channel.Subscribe(
                    (v) =>
                    {
                        if (channel.LatestAuthor != "RedisOther")
                            syncObs.OnNext(KeyValuePair.Create(channel.Components.OfType<RedisKey>().FirstOrDefault(), v));
                    },
                    (ex) =>
                    {
                        syncObs.OnError(ex);
                    },
                    () => 
                    {
                        syncObs.OnCompleted();
                    });      
            });
        }
    }
}
