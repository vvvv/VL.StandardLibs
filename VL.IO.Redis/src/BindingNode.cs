using System;
using System.Reactive.Disposables;
using VL.Core;
using VL.Core.Import;
using VL.Model;
using VL.Lib.Reactive;
using VL.IO.Redis.Internal;
using Microsoft.Extensions.Logging;

namespace VL.IO.Redis
{
    [ProcessNode(Name = "Binding")]
    public class BindingNode : IDisposable
    {
        private readonly SerialDisposable _current = new();
        private readonly NodeContext _nodeContext;
        private readonly ILogger _logger;

        private (RedisClient? client, IChannel? channel, string? key, Initialization initialization, 
            RedisBindingType bindingType, CollisionHandling collisionHandling, Optional<SerializationFormat> serializationFormat,
            Optional<TimeSpan> expiry) _config;

        public BindingNode([Pin(Visibility = PinVisibility.Hidden)] NodeContext nodeContext)
        {
            _nodeContext = nodeContext;
            _logger = nodeContext.GetLogger();
        }

        public void Update(
            RedisClient? client, 
            IChannel? channel, 
            string? key,
            Initialization initialization = Initialization.Redis,
            RedisBindingType bindingType = RedisBindingType.SendAndReceive,
            CollisionHandling collisionHandling = default,
            Optional<SerializationFormat> serializationFormat = default,
            Optional<TimeSpan> expiry = default)
        {
            var config = (client, channel, key, initialization, bindingType, collisionHandling, serializationFormat, expiry);
            if (config == _config)
                return;

            _config = config;
            _current.Disposable = null;

            if (client is null || channel is null || string.IsNullOrWhiteSpace(key))
                return;

            var model = new BindingModel(key, initialization, bindingType, collisionHandling, serializationFormat.ToNullable(), expiry.ToNullable());
            try
            {
                _current.Disposable = client.AddBinding(model, channel, logger: _logger);
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
