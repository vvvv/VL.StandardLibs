using VL.Core.Reactive;
using VL.Lib.Reactive;

namespace VL.IO.Redis.Internal
{
    // Called from UI code
    public static class InternalAPI
    {
        public static void AddBinding(RedisClient input, IChannel channel, BindingModel bindingModel) => input.AddPersistentBinding(channel, bindingModel);
        public static void RemoveBinding(RedisClient input, IBinding binding) => input.RemovePersistentBinding(binding);
    }
}
