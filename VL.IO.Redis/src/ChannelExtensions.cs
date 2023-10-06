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
using VL.Core.Reactive;
using VL.Lib.Reactive;

namespace VL.IO.Redis
{

    public static class ChannelExtensions
    {
        public static void EnsureSingleRedisBinding(this IChannel channel, 
            IRedisBindingModel redisBindingModel,
            IRedisModule redisModule,
            Func<RedisBinding, IDisposable> transaction)
        {
            EnsureSingleRedisBinding(channel, new RedisBinding(channel, redisBindingModel, redisModule, transaction));
        }
        public static void EnsureSingleRedisBinding(this IChannel channel,
            IRedisBindingModel redisBindingModel,
            IRedisModule redisModule,
            Func<RedisBinding, IDisposable> transaction,
            out IRedisBindingModel RedisBindingModel)
        {
            RedisBindingModel = redisBindingModel;
            EnsureSingleRedisBinding(channel, new RedisBinding(channel, redisBindingModel, redisModule, transaction));
        }

        public static void EnsureSingleRedisBinding(this IChannel channel,
            IRedisBindingModel redisBindingModel,
            IRedisModule redisModule,
            Func<RedisBinding, IDisposable> transaction,
            out RedisBinding RedisBinding)
        {
            RedisBinding = new RedisBinding(channel, redisBindingModel, redisModule, transaction);
            EnsureSingleRedisBinding(channel, RedisBinding);
        }

        public static void EnsureSingleRedisBinding(this IChannel channel, RedisBinding redisBinding)
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

            var binding = channel.Components.OfType<RedisBinding>();

            if (binding.Any())
            {
                if (binding.Count() == 1)
                {
                    if (binding.FirstOrDefault() ==  redisBinding)
                    {
                        return;
                    }
                    else
                    {
                        channel.Components = channel.Components.Replace(binding.First(), redisBinding);
                        return;
                    }
                }
                else
                {
                    var builder = channel.Components.ToBuilder();
                    
                    foreach (var k in binding)
                    {
                        builder.Remove(k);
                        k.Dispose();
                    }
                    builder.Add(redisBinding);
                    channel.Components = builder.ToImmutable();
                    return;
                }
            }
            else 
            {
                channel.Components = channel.Components.Add(redisBinding);
                return;
            }
        }

        public static IChannel TryGetRedisBinding(this IChannel channel, out bool success, out RedisBinding redisBindingModel)
        {
            redisBindingModel = channel.Components.OfType<RedisBinding>().FirstOrDefault();
            success = redisBindingModel != null;
            return channel;
        }

        public static IChannel GetRedisResult(this IChannel channel, out bool OnSuccessfulWrite, out bool OnSuccessfulRead, out bool OnRedisOverWrite)
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
    }
}
