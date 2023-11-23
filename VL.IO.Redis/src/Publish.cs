using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using VL.Core;
using VL.Core.Import;
using VL.Model;

namespace VL.IO.Redis
{
    // TODO: Fix node name - has stupid `1 inside!
    [ProcessNode(Name = "Publish")]
    public class Publish<T> : IDisposable
    {
        private readonly SerialDisposable _subscription = new();
        private readonly ILogger _logger;

        private (RedisClient? client, string? redisChannel, IObservable<T>? value, RedisChannel.PatternMode pattern, SerializationFormat? format) _config;

        // TODO: For unit testing it would be nice to take the logger directly!
        public Publish([Pin(Visibility = PinVisibility.Hidden)] NodeContext nodeContext)
        {
            _logger = nodeContext.GetLogger();
        }

        public void Dispose() 
        {
            _subscription.Dispose();
        }

        public void Update(
            RedisClient? client, 
            string? redisChannel, 
            IObservable<T>? input, 
            RedisChannel.PatternMode pattern = RedisChannel.PatternMode.Auto, 
            SerializationFormat? serializationFormat = default)
        {
            var config = (client, redisChannel, input, pattern, serializationFormat);
            if (config == _config)
                return;

            _config = config;
            _subscription.Disposable = null;

            if (client is null || redisChannel is null || input is null)
                return;

            _subscription.Disposable = input.Subscribe(v =>
            {
                try
                {
                    var subscriber = client.GetSubscriber();
                    var channel = new RedisChannel(redisChannel, pattern);
                    var value = client.Serialize(v, serializationFormat);
                    subscriber.Publish(channel, value, CommandFlags.FireAndForget);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Exception while publishing.");
                }
            });
        }
    }
}
