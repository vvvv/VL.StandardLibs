﻿namespace VL.IO.Redis.Internal
{
    /// <summary>
    /// Allows to take part in a Redis transaction created each vvvv frame by the <see cref="RedisClientInternal"/>.
    /// </summary>
    interface IParticipant
    {
        void BuildUp(RedisClientInternal redisClientInternal, TransactionBuilder builder);
        void Invalidate(string key);
    }
}
