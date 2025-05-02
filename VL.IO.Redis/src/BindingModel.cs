﻿using StackExchange.Redis;
using System;

namespace VL.IO.Redis
{
    /// <summary>
    /// A description of a Redis binding. The <see cref="RedisClientManager"/> holds one of these per global channel.
    /// </summary>
    /// <param name="Key">The Redis key.</param>
    /// <param name="Initialization">What to do on startup.</param>
    /// <param name="BindingType">Defines the direction of the binding.</param>
    /// <param name="CollisionHandling">Defines the behavior when both Redis and vvvv have a value.</param>
    /// <param name="SerializationFormat">The serialization format to used for this binding. If not specified the one from the <see cref="RedisClientInternal"/> is used.</param>
    /// <param name="Expiry">Allows to make this key expire (and vanish) from the Redis database. The channel will persist and will pick up values as soon as the key in the Db exists again.</param>
    /// <param name="When">Which condition to set the value under (defaults to always).</param>
    public record struct BindingModel(
        string Key, 
        Initialization Initialization = Initialization.Redis,
        BindingDirection BindingType = BindingDirection.InOut, 
        CollisionHandling CollisionHandling = default, 
        SerializationFormat? SerializationFormat = default,
        TimeSpan? Expiry = null,
        When When = When.Always);
}
