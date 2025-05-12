using System;
using System.Reactive.Disposables;
using VL.Core;
using VL.Core.Import;
using VL.Model;
using VL.Lib.Reactive;
using VL.IO.Redis.Internal;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace VL.IO.Redis
{
    /// <summary>
    /// Binds a Channel to a key in a Redis database
    /// </summary>
    [ProcessNode(Name = "Binding")]
    public class BindingNode : IDisposable
    {
        private readonly SerialDisposable _current = new();
        private readonly NodeContext _nodeContext;
        private readonly ILogger _logger;

        private (RedisClient? client, IChannel? input, string? key, Initialization initialization, 
            BindingDirection bindingType, CollisionHandling collisionHandling, Optional<SerializationFormat> serializationFormat,
            Optional<TimeSpan> expiry, When when) _config;

        public BindingNode([Pin(Visibility = PinVisibility.Hidden)] NodeContext nodeContext)
        {
            _nodeContext = nodeContext;
            _logger = nodeContext.GetLogger();
        }

        /// <param name="client">The Redis client.</param>
        /// <param name="key">The Redis key.</param>
        /// <param name="input">The channel to bind to.</param>
        /// <param name="bindingDirection">Defines the direction of the binding.</param>
        /// <param name="initialization">What to do on startup.</param>
        /// <param name="collisionHandling">Defines the behavior when both Redis and vvvv have a value.</param>
        /// <param name="serializationFormat">The serialization format to use for this binding. If not specified the one from the <paramref name="client"/> is used.</param>
        /// <param name="expiry">Allows to make this key expire (and vanish) from the Redis database. The channel will persist and will pick up values as soon as the key in the Db exists again.</param>
        /// <param name="when">Which condition to set the value under (defaults to always).</param>
        public void Update(
            RedisClient? client, 
            string? key,
            IChannel? input, 
            BindingDirection bindingDirection = BindingDirection.InOut,
            Initialization initialization = Initialization.Redis,
            CollisionHandling collisionHandling = default,
            Optional<SerializationFormat> serializationFormat = default,
            Optional<TimeSpan> expiry = default,
            When when = When.Always)
        {
            var config = (client, input, key, initialization, bindingDirection, collisionHandling, serializationFormat, expiry, when);
            if (config == _config)
                return;

            _config = config;
            _current.Disposable = null;

            if (client is null || input is null || string.IsNullOrWhiteSpace(key))
                return;

            var model = new BindingModel(key, initialization, bindingDirection, collisionHandling, serializationFormat.ToNullable(), expiry.ToNullable(), when, CreatedViaNode: true);
            try
            {
                _current.Disposable = client.AddBinding(model, input, client._module, logger: _logger);
            }
            catch (Exception e)
            {
                // TODO: Use logger / add better API which also works in exported apps
                _current.Disposable = IVLRuntime.Current?.AddException(_nodeContext, e);
            }
        }

        public void Dispose()
        {
            _current.Dispose();
        }
    }
}
