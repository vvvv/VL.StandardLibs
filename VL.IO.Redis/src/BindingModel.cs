namespace VL.IO.Redis
{
    /// <summary>
    /// A description of a Redis binding. The <see cref="RedisModule"/> holds one of these per global channel.
    /// </summary>
    /// <param name="Key">The Redis key.</param>
    /// <param name="Initialization">What to do on startup.</param>
    /// <param name="BindingType">Defines the direction of the binding.</param>
    /// <param name="CollisionHandling">Defines the behaviour when both Redis and vvvv have a value.</param>
    /// <param name="SerializationFormat">The serialization format to used for this binding. If not specified the one from the <see cref="RedisClient"/> is used.</param>
    public record struct BindingModel(
        string Key, 
        Initialization Initialization = Initialization.Redis,
        RedisBindingType BindingType = RedisBindingType.SendAndReceive, 
        CollisionHandling CollisionHandling = default, 
        SerializationFormat? SerializationFormat = default);
}
