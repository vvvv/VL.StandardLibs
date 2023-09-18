using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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

    public record RedisBindingRuntime : IModule
    {
        public string Name => "Redis";
        public string Description => "Bind a Channel to Redis";
        public bool SupportsType(Type type) => true;

        public readonly IObservable<RedisCommandQueue> AfterFrame;
        public readonly IObservable<ImmutableDictionary<Guid, object>> BeforFrame;

        public RedisBindingRuntime
        (
            IObservable<RedisCommandQueue> AfterFrame,
            IObservable<ImmutableDictionary<Guid, object>> BeforFrame
        )
        {
            this.AfterFrame = AfterFrame;
            this.BeforFrame = BeforFrame;
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
    }

    public record RedisBinding : IBinding
    {
        public readonly Guid setID;
        public readonly Guid getID;

        #region Model
        public readonly RedisBindingModel Model;
        public RedisKey Key => Model.Key;
        public RedisBindingType BindingType => Model.BindingType;
        public Initialisation Initialisation => Model.Initialisation;
        public CollisionHandling CollisionHandling => Model.CollisionHandling;
        public string ChannelPath => Model.ChannelPath;
        #endregion Model

        #region Runtime
        public readonly RedisBindingRuntime Runtime;
        public IObservable<RedisCommandQueue> AfterFrame => Runtime.AfterFrame;
        public IObservable<ImmutableDictionary<Guid, object>> BeforFrame => Runtime.BeforFrame;
        #endregion Runtime
        
        #region IBinding
        IModule IBinding.Module => Runtime;
        public string Description => Runtime.Description;
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
            string Key,
            RedisBindingType BindingType,
            Initialisation Initialisation,
            CollisionHandling CollisionHandling,
            string ChannelPath,
            IObservable<RedisCommandQueue> AfterFrame,
            IObservable<ImmutableDictionary<Guid, object>> BeforFrame
        )
        {
            this.Model = new RedisBindingModel(Key, BindingType, Initialisation, CollisionHandling, ChannelPath);
            this.Runtime = new RedisBindingRuntime(AfterFrame, BeforFrame);
            this.setID = Guid.NewGuid();
            this.getID = Guid.NewGuid();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
