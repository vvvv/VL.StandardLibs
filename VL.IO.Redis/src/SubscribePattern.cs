using Microsoft.Extensions.Logging;
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
    /// Subscribe using a glob-style pattern to receive value changes from a range of Redis channels
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [ProcessNode(Name = "Subscribe (Pattern)")]
    public class SubscribePattern<T> : IDisposable
    {
        private readonly SerialDisposable _subscription = new();
        private readonly Subject<ChannelMessage<T?>> _subject = new();
        private readonly ILogger _logger;

        record struct Config(RedisClient? Client, string? Pattern, SerializationFormat? Format, bool ProcessMessagesConcurrently);

        private Config _config;

        // TODO: For unit testing it would be nice to take the logger directly!
        public SubscribePattern([Pin(Visibility = PinVisibility.Hidden)] NodeContext nodeContext)
        {
            _logger = nodeContext.GetLogger();
        }

        public void Dispose()
        {
            _subscription.Dispose();
        }

        [return: Pin(Name = "Output")]
        public IObservable<ChannelMessage<T?>> Update(
            RedisClient? client,
            string? pattern = null,
            Optional<SerializationFormat> serializationFormat = default)
        {
            var config = new Config(client, pattern, serializationFormat.ToNullable(), ProcessMessagesConcurrently: false);
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
            if (client is null || string.IsNullOrEmpty(config.Pattern))
                return;

            var subscriber = client.GetSubscriber();
            var channel = new RedisChannel(config.Pattern, RedisChannel.PatternMode.Pattern);

            if (config.ProcessMessagesConcurrently)
            {
                Action<RedisChannel, RedisValue> handler = (redisChannel, redisValue) =>
                {
                    try
                    {
                        var value = client.Deserialize<T>(redisValue, config.Format);
                        _subject.OnNext(new (redisChannel.ToString(), value));
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
                        _subject.OnNext(new ChannelMessage<T?>(channelMessage.Channel.ToString(), value));
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
