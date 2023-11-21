using VL.Core.Reactive;

namespace VL.IO.Redis.Internal
{
    /// <summary>
    /// A non-generic view on <see cref="Binding{T}"/>. Used by <see cref="RedisModule"/> to manage bindings.
    /// </summary>
    public interface IRedisBinding : IBinding
    {
        public BindingModel Model { get; }
    }
}
