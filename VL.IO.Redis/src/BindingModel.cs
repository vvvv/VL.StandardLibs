using StackExchange.Redis;
using Stride.Core.Extensions;
using System;
using System.Text;
using VL.Core;
using VL.IO.Redis.Experimental;
using VL.Lib.Reactive;

namespace VL.IO.Redis
{
    /// <summary>
    /// A description of a Redis binding. The <see cref="RedisClientManager"/> holds one of these per global channel.
    /// </summary>
    /// <param name="Key">The Redis key.</param>
    /// <param name="Initialization">What to do on startup.</param>
    /// <param name="BindingType">Defines the direction of the binding.</param>
    /// <param name="CollisionHandling">Defines the behavior when both Redis and vvvv have a value.</param>
    /// <param name="SerializationFormat">The serialization format to used for this binding. If not specified the one from the <see cref="RedisClient"/> is used.</param>
    /// <param name="Expiry">Allows to make this key expire (and vanish) from the Redis database. The channel will persist and will pick up values as soon as the key in the Db exists again.</param>
    /// <param name="When">Which condition to set the value under (defaults to always).</param>
    public readonly record struct BindingModel(
        Optional<string> Key = default,
        Optional<Initialization> Initialization = default, //Initialization.Redis
        Optional<BindingDirection> BindingType = default, //BindingDirection.InOut
        Optional<CollisionHandling> CollisionHandling = default,
        Optional<SerializationFormat> SerializationFormat = default, //SerializationFormat.MessagePack
        Optional<TimeSpan> Expiry = default,
        Optional<When> When = default, //When.Always,
        bool CreatedViaNode = false)
    {
        public string ResolveKey(IChannel channel) => ResolveKey(channel.Path);
        public string ResolveKey(string? channelName)
        {
            var newSchool = Key.TryGetValue(channelName ?? "");
            
            if (!newSchool.IsNullOrEmpty())
                return newSchool;

            // old model probably stored key = "". We still want to resolve channelName
            return channelName ?? "";
        }

        Initialization ResolvedInitialization(ResolvedBindingModel m) => Initialization.TryGetValue(m.Initialization);
        BindingDirection ResolvedBindingType(ResolvedBindingModel m) => BindingType.TryGetValue(m.BindingType);
        CollisionHandling ResolvedCollisionHandling(ResolvedBindingModel m) => CollisionHandling.TryGetValue(m.CollisionHandling);
        SerializationFormat ResolvedSerializationFormat(ResolvedBindingModel m) => SerializationFormat.TryGetValue(m.SerializationFormat);
        TimeSpan? ResolvedExpiry(ResolvedBindingModel m) => Expiry.HasValue ? Expiry.Value : m.Expiry;
        When ResolvedWhen(ResolvedBindingModel m) => When.TryGetValue(m.When);

        public ResolvedBindingModel Resolve(RedisModule m, IChannel channel)
        {
            var model = m.Model;
            return new ResolvedBindingModel(
                this,
                Key: ResolveKey(channel),
                PublicChannelPath: channel.Path,
                Initialization: ResolvedInitialization(model),
                BindingType: ResolvedBindingType(model),
                CollisionHandling: ResolvedCollisionHandling(model),
                SerializationFormat: ResolvedSerializationFormat(model),
                Expiry: ResolvedExpiry(model),
                When: ResolvedWhen(model),
                CreatedViaNode: CreatedViaNode);
        }

        override public string ToString()
        {
            var b = new StringBuilder();

            if (Key.HasValue)
                b.Append($"Key: {Key}, ");
            if (Initialization.HasValue)
                b.Append($"Initialization: {Initialization}, ");
            if (BindingType.HasValue)
                b.Append($"BindingType: {BindingType}, ");
            if (CollisionHandling.HasValue)
                b.Append($"CollisionHandling: {CollisionHandling}, ");
            if (SerializationFormat.HasValue)
                b.Append($"SerializationFormat: {SerializationFormat}, ");
            if (Expiry.HasValue)
                b.Append($"Expiry: {Expiry}, ");
            if (When.HasValue)
                b.Append($"When: {When}, ");

            var s = b.ToString();
            if (!s.IsNullOrEmpty())
                s = s.Remove(s.Length - 2, 2); // remove last ", "
            else
                s = "Enabled, not tweaked";

            return s;
        }
    }

    public readonly record struct ResolvedBindingModel(
        BindingModel Model,
        string? PublicChannelPath,
        string? Key,
        Initialization Initialization = Initialization.Redis,
        BindingDirection BindingType = BindingDirection.InOut,
        CollisionHandling CollisionHandling = default,
        SerializationFormat SerializationFormat = SerializationFormat.MessagePack,
        TimeSpan? Expiry = default,
        When When = When.Always,
        bool CreatedViaNode = false)
    {
        public override string ToString()
        {
            var e = Expiry.HasValue ? Expiry.Value.ToString() : "not expiring";
            return
$@"Redis Key: {Key},
Initialization: {Initialization}, 
BindingType: {BindingType}, 
CollisionHandling: {CollisionHandling}, 
SerializationFormat: {SerializationFormat}, 
Expiry: {e}, 
When: {When}";
        }
    }
}
