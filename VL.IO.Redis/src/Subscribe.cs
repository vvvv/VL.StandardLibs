﻿using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using VL.Core;
using VL.Core.Import;
using VL.Model;

namespace VL.IO.Redis
{
    /// <summary>
    /// Subscribe to receive value changes on a specified Redis Channel
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [ProcessNode]
    public class Subscribe<T> : IDisposable
    {
        private readonly SerialDisposable _subscription = new();
        private readonly Subject<T?> _subject = new();
        private readonly ILogger _logger;

        record struct Config(RedisClient? Client, string? Channel, SerializationFormat? Format, bool ProcessMessagesConcurrently);

        private Config _config;

        // TODO: For unit testing it would be nice to take the logger directly!
        public Subscribe([Pin(Visibility = PinVisibility.Hidden)] NodeContext nodeContext)
        {
            _logger = nodeContext.GetLogger();
        }

        public void Dispose()
        {
            _subscription.Dispose();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="channel">Name of the Redis channel</param>
        /// <param name="serializationFormat"></param>
        /// <returns></returns>
        [return: Pin(Name = "Output")]
        public IObservable<T?> Update(
            RedisClient? client,
            string? channel,
            Optional<SerializationFormat> serializationFormat = default)
        {
            var config = new Config(client, channel, serializationFormat.ToNullable(), ProcessMessagesConcurrently: false);
            if (config != _config)
            {
                _config = config;
                Resubscribe(config);
            }
            return _subject;
        }

        private void Resubscribe(Config config)
        {
            // Unsubscribe
            _subscription.Disposable = null;

            var client = config.Client;
            if (client is null || string.IsNullOrEmpty(config.Channel))
                return;

            var subscriber = client.InternalRedisClient?.GetSubscriber();
            var channel = new RedisChannel(config.Channel, RedisChannel.PatternMode.Literal);

            if (config.ProcessMessagesConcurrently)
            {
                Action<RedisChannel, RedisValue> handler = (redisChannel, redisValue) =>
                {
                    try
                    {
                        var value = client.Deserialize<T>(redisValue, config.Format);
                        _subject.OnNext(value);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Unexpected exception in subscribe.");
                    }
                };
                subscriber.Subscribe(channel, handler);
                _subscription.Disposable = Disposable.Create(() => subscriber.Unsubscribe(channel, handler));
            }
            else
            {
                Action<ChannelMessage> handler = (channelMessage) =>
                {
                    try
                    {
                        var redisValue = channelMessage.Message;
                        var value = client.Deserialize<T>(redisValue, config.Format);
                        _subject.OnNext(value);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Unexpected exception in subscribe.");
                    }
                };

                var queue = subscriber.Subscribe(channel);
                queue.OnMessage(handler);
                _subscription.Disposable = Disposable.Create(() => queue.Unsubscribe());
            }
        }
    }
}
