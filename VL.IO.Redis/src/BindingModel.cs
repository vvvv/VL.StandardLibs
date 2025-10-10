using StackExchange.Redis;
using Stride.Core.Extensions;
using System;
using System.Text;
using VL.Core;
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

        Initialization ResolvedInitialization(ResolvedBindingModel m, ref bool changed)
        {
            Initialization.Incorporate(m.Initialization, out var value, out var isDefault, out _);
            if (!isDefault)
                changed = true;
            return value;
        }

        BindingDirection ResolvedBindingType(ResolvedBindingModel m, ref bool changed)
        {
            BindingType.Incorporate(m.BindingType, out var value, out var isDefault, out _);
            if (!isDefault)
                changed = true;
            return value;
        }
        CollisionHandling ResolvedCollisionHandling(ResolvedBindingModel m, ref bool changed)
        {
            CollisionHandling.Incorporate(m.CollisionHandling, out var value, out var isDefault, out _);
            if (!isDefault)
                changed = true;
            return value;
        }
        SerializationFormat ResolvedSerializationFormat(ResolvedBindingModel m, ref bool changed)
        {
            SerializationFormat.Incorporate(m.SerializationFormat, out var value, out var isDefault, out _);
            if (!isDefault)
                changed = true;
            return value;
        }
        TimeSpan? ResolvedExpiry(ResolvedBindingModel m, ref bool changed)
        {
            var value = Expiry.HasValue? Expiry.Value: m.Expiry;
            if (value != m.Expiry)
                changed = true;
            return value;
        }
        When ResolvedWhen(ResolvedBindingModel m, ref bool changed)
        {
            When.Incorporate(m.When, out var value, out var isDefault, out _);
            if (!isDefault)
                changed = true;
            return value;
        }

        public ResolvedBindingModel Resolve(RedisClient? client, IChannel? channel)
        {
            var model = client != null ? client.Model : default;
            bool changed = false;
            return new ResolvedBindingModel(
                this,
                Key: channel != null ? ResolveKey(channel) : string.Empty,
                PublicChannelPath: channel?.Path,
                Initialization: ResolvedInitialization(model, ref changed),
                BindingType: ResolvedBindingType(model, ref changed),
                CollisionHandling: ResolvedCollisionHandling(model, ref changed),
                SerializationFormat: ResolvedSerializationFormat(model, ref changed),
                Expiry: ResolvedExpiry(model, ref changed),
                When: ResolvedWhen(model, ref changed),
                CreatedViaNode: CreatedViaNode,
                IsDefault: !changed);
        }

        override public string ToString()
        {
            var b = new StringBuilder();

            if (Key.HasValue)
                b.AppendLine($"Redis Key: {Key}");
            if (Initialization.HasValue)
                b.AppendLine($"Initialization: {Initialization}");
            if (BindingType.HasValue)
                b.AppendLine($"BindingType: {BindingType}");
            if (CollisionHandling.HasValue)
                b.AppendLine($"CollisionHandling: {CollisionHandling}");
            if (SerializationFormat.HasValue)
                b.AppendLine($"SerializationFormat: {SerializationFormat}");
            if (Expiry.HasValue)
                b.AppendLine($"Expiry: {Expiry}");
            if (When.HasValue)
                b.AppendLine($"When: {When}");

            var s = b.ToString();
            if (s.IsNullOrEmpty())
                s = "Enabled, not tweaked";

            return s;
        }
    }

    public readonly record struct ResolvedBindingModel(
        BindingModel Model,
        string? PublicChannelPath,
        string Key,
        Initialization Initialization = Initialization.Redis,
        BindingDirection BindingType = BindingDirection.InOut,
        CollisionHandling CollisionHandling = default,
        SerializationFormat SerializationFormat = SerializationFormat.MessagePack,
        TimeSpan? Expiry = default,
        When When = When.Always,
        bool CreatedViaNode = false,
        bool IsDefault = false)
    {

        public override string ToString()
        {
            var e = Expiry.HasValue ? Expiry.Value.ToString() : "not expiring";
            return
$@"Redis Key: {Key}
Initialization: {Initialization}
BindingType: {BindingType}
CollisionHandling: {CollisionHandling}
SerializationFormat: {SerializationFormat}
Expiry: {e}
When: {When}";
        }
    }
}
