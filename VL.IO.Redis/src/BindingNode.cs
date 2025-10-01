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
    [ProcessNode(Name = "BindToRedis")]
    public class BindingNode : IDisposable
    {
        private readonly SerialDisposable _current = new();
        private readonly NodeContext _nodeContext;
        private readonly ILogger _logger;
        private ResolvedBindingModel _latestResolvedModel;

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
            IChannel? input, 
            Optional<string> key,
            Optional<BindingDirection> bindingDirection = default,
            Optional<Initialization> initialization = default,
            [Pin(Visibility = PinVisibility.Optional)] Optional<CollisionHandling> collisionHandling = default,
            Optional<SerializationFormat> serializationFormat = default,
            Optional<TimeSpan> expiry = default,
            Optional<When> when = default)
        {
            if (client is null || input is null)
                return;

            var model = new BindingModel(key, initialization, bindingDirection, collisionHandling, serializationFormat, expiry, when, CreatedViaNode: true);
            var resolvedBindingModel = model.Resolve(client._module, input);
            if (resolvedBindingModel == _latestResolvedModel)
                return;
            _latestResolvedModel = resolvedBindingModel;

            _current.Disposable = null;

            try
            {
                _current.Disposable = client.AddBinding(resolvedBindingModel, input, client._module, logger: _logger);
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
