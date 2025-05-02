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
    /// <summary>
    /// Publish a message on a specified Redis Channel. The Message will not saved in the database!
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [ProcessNode]
    public class Publish<T> : IDisposable
    {
        private readonly SerialDisposable _subscription = new();
        private readonly ILogger _logger;

        private (RedisClient? client, ConnectionMultiplexer? Multiplexer, string? redisChannel, IObservable<T>? value, SerializationFormat? format) _config;

        // TODO: For unit testing it would be nice to take the logger directly!
        public Publish([Pin(Visibility = PinVisibility.Hidden)] NodeContext nodeContext)
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
        /// <param name="input"></param>
        /// <param name="serializationFormat"></param>
        public void Update(
            RedisClient? client, 
            string? channel, 
            IObservable<T>? input, 
            Optional<SerializationFormat> serializationFormat = default)
        {
            var config = (client, client?.InternalRedisClient?.Multiplexer, channel, input, format: serializationFormat.ToNullable());
            if (config == _config)
                return;

            _config = config;
            _subscription.Disposable = null;

            if (client?.InternalRedisClient is null || string.IsNullOrEmpty(channel) || input is null)
                return;

            _subscription.Disposable = input.Subscribe(v =>
            {
                try
                {
                    var subscriber = client.InternalRedisClient.GetSubscriber();
                    var redisChannel = new RedisChannel(channel, RedisChannel.PatternMode.Literal);
                    var value = client.Serialize(v, config.format);
                    subscriber.Publish(redisChannel, value, CommandFlags.FireAndForget);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Exception while publishing.");
                }
            });
        }
    }
}
