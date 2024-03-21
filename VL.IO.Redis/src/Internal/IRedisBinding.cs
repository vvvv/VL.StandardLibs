using VL.Core.Reactive;

namespace VL.IO.Redis.Internal
{
    /// <summary>
    /// A non-generic view on <see cref="Binding{T}"/>. Used by <see cref="RedisClientManager"/> to manage bindings.
    /// </summary>
    public interface IRedisBinding : IBinding
    {
        public string? ChannelName { get; }
        public BindingModel Model { get; }
    }
}
