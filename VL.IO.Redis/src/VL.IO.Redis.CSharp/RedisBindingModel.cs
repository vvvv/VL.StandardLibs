using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Core.Reactive;

namespace VL.IO.Redis
{
    public class RedisResult
    {
        public bool OnSuccessfulWrite{ get; internal set; }
        public bool OnSuccessfulRead { get; internal set; }
        public bool OnRedisOverWrite { get; internal set; }
    }

    public enum Initialisation
    {
        None = 0,
        Local = 1,
        Redis = 2,
    }

    public enum CollisionHandling
    {
        None = 0,
        LocalWins = 1,
        RedisWins = 2,
    }

    public record RedisBindingModel
    {
        public readonly RedisKey Key;
        public readonly BindingType BindingType;
        public readonly Initialisation Initialisation;
        public readonly CollisionHandling CollisionHandling;
        public readonly string ChannelPath;

        public readonly IObservable<RedisCommandQueue> AfterFrame;
        public readonly IObservable<ImmutableDictionary<Guid, object>> BeforFrame;

        public readonly Guid setID;
        public readonly Guid getID;

        public RedisBindingModel
        (
            string Key,
            BindingType BindingType,
            Initialisation Initialisation,
            CollisionHandling CollisionHandling,
            string ChannelPath,
            IObservable<RedisCommandQueue> AfterFrame,
            IObservable<ImmutableDictionary<Guid, object>> BeforFrame
        )
        {
            this.Key = Key;
            this.BindingType = BindingType;
            this.Initialisation = Initialisation;
            this.CollisionHandling = CollisionHandling;
            this.ChannelPath = ChannelPath;
            this.AfterFrame = AfterFrame;
            this.BeforFrame = BeforFrame;
            this.setID = Guid.NewGuid();
            this.getID = Guid.NewGuid();
        }
    }
}
