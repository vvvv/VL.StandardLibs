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
            var result = channel.Components.OfType<RedisResult>();

            if (result.Any())
            {
                if (result.Count() > 1)

                {
                    var builder = channel.Components.ToBuilder();

                    foreach (var k in result)
                    {
                        builder.Remove(k);
                    }
                    builder.Add(new RedisResult());
                    channel.Components = builder.ToImmutable();
                }
            }
            else
            {
                channel.Components = channel.Components.Add(new RedisResult());
            }

            var binding = channel.Components.OfType<RedisBindingModel>();

            if (binding.Any())
            {
                if (binding.Count() == 1)
                {
                    if (binding.FirstOrDefault() ==  redisBinding)
                    {
                        return channel;
                    }
                    else
                    {
                        channel.Components = channel.Components.Replace(binding.First(), redisBinding);
                        return channel;
                    }
                }
                else
                {
                    var builder = channel.Components.ToBuilder();
                    
                    foreach (var k in binding)
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

        public static IChannel<T> GetRedisResult<T>(this IChannel<T> channel, out bool OnSuccessfulWrite, out bool OnSuccessfulRead, out bool OnRedisOverWrite)
        {
            var result = channel.Components.OfType<RedisResult>().FirstOrDefault();
            if (result != null)
            {
                OnSuccessfulWrite = result.OnSuccessfulWrite;
                OnSuccessfulRead = result.OnSuccessfulRead;
                OnRedisOverWrite = result.OnRedisOverWrite;
            }
            else
            {
                OnSuccessfulWrite = false;
                OnSuccessfulRead = false;
                OnRedisOverWrite = false;
            }
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
