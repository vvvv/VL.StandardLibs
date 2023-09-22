using StackExchange.Redis;
using System;
using System.Collections.Immutable;
using VL.Core.Reactive;
using VL.Lib.Reactive;

namespace VL.IO.Redis
{
    public class RedisResult
    {
        public bool OnSuccessfulWrite{ get; internal set; }
        public bool OnSuccessfulRead { get; internal set; }
        public bool OnRedisOverWrite { get; internal set; }
    }

    public enum RedisBindingType 
    {
        None = 0,
        Send = 1,
        Receive = 2,
        SendAndReceive = Send | Receive,
        AlwaysReceive = 8,
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

    public interface IRedisModule : IModule
    {
        public IObservable<RedisCommandQueue> AfterFrame { get; }
        public IObservable<ImmutableDictionary<Guid,object>> BeforFrame { get; }
    }

    public record RedisModuleRuntime : IRedisModule
    {
        public string Name => "Redis";
        public string Description => "Bind a Channel to Redis";
        public bool SupportsType(Type type) => true;

        private readonly IObservable<RedisCommandQueue> afterFrame;
        public IObservable<RedisCommandQueue> AfterFrame => afterFrame;

        private readonly IObservable<ImmutableDictionary<Guid, object>> beforFrame;
        public IObservable<ImmutableDictionary<Guid, object>> BeforFrame => beforFrame;

        public RedisModuleRuntime
        (
            IObservable<RedisCommandQueue> AfterFrame,
            IObservable<ImmutableDictionary<Guid, object>> BeforFrame
        )
        {
            this.afterFrame = AfterFrame;
            this.beforFrame = BeforFrame;
        }
    }

    public record RedisBindingModel
    {
        public readonly RedisKey Key;
        public readonly RedisBindingType BindingType;
        public readonly Initialisation Initialisation;
        public readonly CollisionHandling CollisionHandling;
        public readonly string ChannelPath;

        public RedisBindingModel
        (
            string Key,
            RedisBindingType BindingType,
            Initialisation Initialisation,
            CollisionHandling CollisionHandling,
            string ChannelPath
        )
        {
            this.Key = Key;
            this.BindingType = BindingType;
            this.Initialisation = Initialisation;
            this.CollisionHandling = CollisionHandling;
            this.ChannelPath = ChannelPath;
        }

        public void Split
        (
            out string Key,
            out RedisBindingType BindingType,
            out Initialisation Initialisation,
            out CollisionHandling CollisionHandling,
            out string ChannelPath
        )
        {
            Key = this.Key;
            BindingType = this.BindingType;
            Initialisation = this.Initialisation;
            CollisionHandling = this.CollisionHandling;
            ChannelPath = this.ChannelPath;
        }
    }

    public record RedisBinding : IBinding
    {
        public readonly Guid setID;
        public readonly Guid getID;
        public readonly IChannel channel;
        public readonly IDisposable transaction;

        #region Model
        public readonly RedisBindingModel Model;
        public RedisKey Key => Model.Key;
        public RedisBindingType BindingType => Model.BindingType;
        public Initialisation Initialisation => Model.Initialisation;
        public CollisionHandling CollisionHandling => Model.CollisionHandling;
        public string ChannelPath => Model.ChannelPath;
        #endregion Model

        #region Module
        public readonly IRedisModule Module;
        public IObservable<RedisCommandQueue> AfterFrame => Module.AfterFrame;
        public IObservable<ImmutableDictionary<Guid, object>> BeforFrame => Module.BeforFrame;
        #endregion Module

        #region IBinding
        IModule IBinding.Module => Module;
        public string Description => Module.Description;
        BindingType IBinding.BindingType
        {
            get {  
                switch (Model.BindingType)
                {
                    case RedisBindingType.None:
                        // code block
                        return VL.Core.Reactive.BindingType.None;
                    case RedisBindingType.Send:
                        return VL.Core.Reactive.BindingType.Send;
                    case RedisBindingType.Receive:
                        return VL.Core.Reactive.BindingType.Receive;
                    case RedisBindingType.SendAndReceive:
                        return VL.Core.Reactive.BindingType.SendAndReceive;
                    case RedisBindingType.AlwaysReceive:
                        return VL.Core.Reactive.BindingType.None;
                    default:
                        return VL.Core.Reactive.BindingType.None;
                }
            }
        }
 
        #endregion IBinding

        public RedisBinding
        (
            IChannel channel,
            RedisBindingModel redisBindingModel,
            IRedisModule redisModule,
            Func<RedisBinding,IDisposable> transaction
        )
        {
            this.channel = channel;
            this.Model = redisBindingModel;
            this.Module = redisModule;
            this.transaction = transaction(this);
            this.setID = Guid.NewGuid();
            this.getID = Guid.NewGuid();
        }

        public void Split
        (
            out string Key,
            out RedisBindingType BindingType,
            out Initialisation Initialisation,
            out CollisionHandling CollisionHandling,
            out string ChannelPath
        )
        {
            Key = this.Model.Key;
            BindingType = this.Model.BindingType;
            Initialisation = this.Model.Initialisation;
            CollisionHandling = this.Model.CollisionHandling;
            ChannelPath = this.Model.ChannelPath;
        }

        public void Dispose()
        {
            transaction?.Dispose();
            channel.RemoveComponent(this);
        }
    }
}
