#nullable enable

namespace VL.IO.Redis
{
    public record struct BindingModel(
        string Key, 
        Initialization Initialization = Initialization.Redis,
        RedisBindingType BindingType = RedisBindingType.SendAndReceive, 
        CollisionHandling CollisionHandling = default, 
        SerializationFormat? SerializationFormat = default);
}
