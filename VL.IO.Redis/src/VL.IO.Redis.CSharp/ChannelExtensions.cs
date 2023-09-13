using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using VL.Lib.Reactive;

namespace VL.IO.Redis
{

    public static class ChannelExtensions
    {
        public static IChannel<T> EnsureSingleRedisBinding<T>(this IChannel<T> channel, RedisBindingModel redisBinding)
        {
            var keys = channel.Components.OfType<RedisBindingModel>();

            if (keys.Any())
            {
                if (keys.Count() == 1)
                {
                    if (keys.FirstOrDefault() ==  redisBinding)
                    {
                        return channel;
                    }
                    else
                    {
                        channel.Components = channel.Components.Replace(keys.First(), redisBinding);
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
                    builder.Add(redisBinding);
                    channel.Components = builder.ToImmutable();
                    return channel;
                }
            }
            else 
            {
                channel.Components = channel.Components.Add(redisBinding);
                return channel;
            }       
        }

        public static IChannel<T> TryGetRedisBinding<T>(this IChannel<T> channel, out bool success, out RedisBindingModel redisBindingModel)
        {
            redisBindingModel = channel.Components.OfType<RedisBindingModel>().FirstOrDefault();
            success = redisBindingModel != null;
            return channel;
        }

        public static IObservable<KeyValuePair<RedisBindingModel, T>> ToKeyValueObservable<T>(this IChannel<T> channel, out IObservable<RedisBindingModel> Model)
        {

            var model = channel.Components.OfType<RedisBindingModel>().FirstOrDefault();

            Model = Observable.Start(
                () =>
                {
                    return model;
                }
            );

            return Observable.Create<KeyValuePair<RedisBindingModel, T>>((obs) =>
            {
                var syncObs = Observer.Synchronize(obs);
                return channel.Subscribe(
                    (v) =>
                    {
                        if (channel.LatestAuthor != "RedisOther")
                            syncObs.OnNext(KeyValuePair.Create(channel.Components.OfType<RedisBindingModel>().FirstOrDefault(), v));
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
