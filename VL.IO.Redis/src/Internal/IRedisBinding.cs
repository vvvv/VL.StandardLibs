using VL.Core.Reactive;

namespace VL.IO.Redis.Internal
{
    /// <summary>
    /// Allows to take part in a Redis transaction created each vvvv frame by the <see cref="RedisClient"/>.
    /// </summary>
    interface IRedisBinding : IBinding
    {
        string RedisKey { get; }
        void Reset();
        void BuildUp(TransactionBuilder builder);
        void Invalidate();
    }
}
