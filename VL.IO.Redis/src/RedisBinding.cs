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

    public enum Initialization
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

    public enum SerializationFormat
    {
        MessagePack,
        Json,
        Raw
    }

    public interface IRedisModule : IModule
    {
        public IObservable<RedisCommandQueue> AfterFrame { get; }
        public IObservable<ImmutableDictionary<Guid,object>> BeforFrame { get; }
        public void RemoveBinding(string ChannelPath);
    }

    public record RedisModuleRuntime : IRedisModule
    {
        public string Name => "Redis";
        public string Description => "Bound to Redis via a Binding node";
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

        public void RemoveBinding(string ChannelPath)
        {
            
        }
    }

    public interface IRedisBindingModel
    {
        public string Key { get; }
        public RedisBindingType BindingType { get; }
        public Initialization Initialisation { get; }
        public CollisionHandling CollisionHandling { get; }
        public string ChannelPath { get; }
    }

    //public record RedisBindingModel : IRedisBindingModel
    //{
    //    public string Key { get; }
    //    public RedisBindingType BindingType{ get; }
    //    public Initialisation Initialisation { get; }
    //    public CollisionHandling CollisionHandling { get; }
    //    public string ChannelPath { get; }

    //    public RedisBindingModel
    //    (
    //        string Key,
    //        RedisBindingType BindingType,
    //        Initialisation Initialisation,
    //        CollisionHandling CollisionHandling,
    //        string ChannelPath
    //    )
    //    {
    //        this.Key = Key;
    //        this.BindingType = BindingType;
    //        this.Initialisation = Initialisation;
    //        this.CollisionHandling = CollisionHandling;
    //        this.ChannelPath = ChannelPath;
    //    }

    //    public void Split
    //    (
    //        out string Key,
    //        out RedisBindingType BindingType,
    //        out Initialisation Initialisation,
    //        out CollisionHandling CollisionHandling,
    //        out string ChannelPath
    //    )
    //    {
    //        Key = this.Key;
    //        BindingType = this.BindingType;
    //        Initialisation = this.Initialisation;
    //        CollisionHandling = this.CollisionHandling;
    //        ChannelPath = this.ChannelPath;
    //    }
    //}

    public record RedisBinding : IBinding
    {
        public readonly Guid setID;
        public readonly Guid getID;
        public readonly IChannel channel;
        public readonly IDisposable transaction;

        #region Model
        public readonly IRedisBindingModel redisBindingModel;
        public RedisKey Key => redisBindingModel.Key;
        public RedisBindingType BindingType => redisBindingModel.BindingType;
        public Initialization Initialisation => redisBindingModel.Initialisation;
        public CollisionHandling CollisionHandling => redisBindingModel.CollisionHandling;
        public string ChannelPath => redisBindingModel.ChannelPath;
        #endregion Model

        #region Module
        public readonly IRedisModule redisModule;
        public IObservable<RedisCommandQueue> AfterFrame => redisModule.AfterFrame;
        public IObservable<ImmutableDictionary<Guid, object>> BeforFrame => redisModule.BeforFrame;
        #endregion Module

        #region IBinding
        IModule IBinding.Module => redisModule;
        public string Description => redisModule.Description;
        BindingType IBinding.BindingType
        {
            get {  
                switch (redisBindingModel.BindingType)
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
            IRedisBindingModel redisBindingModel,
            IRedisModule redisModule,
            Func<RedisBinding,IDisposable> transaction
        )
        {
            this.channel = channel;
            this.redisBindingModel = redisBindingModel;
            this.redisModule = redisModule;
            this.transaction = transaction(this);
            this.setID = Guid.NewGuid();
            this.getID = Guid.NewGuid();
        }

        public void Split
        (
            out string Key,
            out RedisBindingType BindingType,
            out Initialization Initialisation,
            out CollisionHandling CollisionHandling,
            out string ChannelPath
        )
        {
            Key = this.redisBindingModel.Key;
            BindingType = this.redisBindingModel.BindingType;
            Initialisation = this.redisBindingModel.Initialisation;
            CollisionHandling = this.redisBindingModel.CollisionHandling;
            ChannelPath = this.redisBindingModel.ChannelPath;
        }

        public void Dispose()
        {
            transaction?.Dispose();
            redisModule.RemoveBinding(redisBindingModel.ChannelPath);
            channel.RemoveComponent(this);
        }
    }
}
