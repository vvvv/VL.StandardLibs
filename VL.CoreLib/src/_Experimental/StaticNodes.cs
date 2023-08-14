using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using VL.Core;
using VL.Lang;
using VL.Lib.Basics.Resources;

namespace VL.Lib.Experimental
{

    public class SingleInstanceHelper<T>
        where T: class
    {
        static readonly ISingleInstanceHelper<T> Impl = AppHost.Global.Services.GetService<IHotswapSpecificNodes>().CreateSingleInstanceHelper<T>();

        public static T SingleInstance(bool forceNewInstance, Func<T> producer, SingleInstanceBehaviorOnStop onStop)
        {
            return Impl.GetInstance(forceNewInstance, producer, onStop);
        }
    }

    public static class ResourceProviderHelper
    {
        // the ungeneric receiver goes well togethr with a Sender<object, object>. This is to avoid confusion. 
        // A channel of a certain name should only exist once and not for each (TChannel, TValue) combination.
        // we don't want to have a fruit receiver being unable to receive the sent apple
        public static void R<TValue>(NodeContext nodeContext /*would be nice to access the app context directly as nodeContext.GetAppContext() consumes a few cycles*/,
            object channel, TValue fallbackValue, out TValue value, out bool success, bool warn = true)
        {
            Sender<object, object>.Receive(nodeContext, channel, out object boxedValue, out success, warn);
            if (success)
            {
                if (boxedValue is TValue v)
                    value = v;
                else
                {
                    IVLRuntime.Current?.AddMessage(nodeContext.Path.Stack.Peek(), boxedValue != null
                        ? $"Sent object is null. Can't convert to {typeof(TValue)}."
                        : $"Sent object is of type {boxedValue?.GetType()}. Can't convert to {typeof(TValue)}.");
                    value = fallbackValue;
                }
            }
            else
                value = fallbackValue;
        }
    }

    /// <summary>
    /// One instance per app and app run. If you restart the app (via F8 and F5) you will get a fresh instance.
    /// </summary>
    public class SingleInstancePerApp<T> : IDisposable, ISwappableGenericType
    {
        IResourceProvider<T> TheOneProviderPerApp;
        IResourceHandle<T> OurHandle;

        public SingleInstancePerApp(Func<T> producer)
        {
            if (producer is null)
                throw new ArgumentNullException(nameof(producer));

            TheOneProviderPerApp = ResourceProvider.NewPooledPerApp(producer);
            OurHandle = TheOneProviderPerApp.GetHandle();
        }

        static SingleInstancePerApp<T> Swap(object value)
        {
            return new SingleInstancePerApp<T>(() => (T)value);
        }

        public T Value => OurHandle.Resource;

        public void Dispose()
        {
            OurHandle.Dispose();
        }

        object ISwappableGenericType.Swap(Type newType, Swapper swapObject)
        {
            var newObject = swapObject(Value, newType.GetGenericArguments()[0]);
            var newSingleInstance =newType.GetMethod(nameof(Swap), 
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                .Invoke(null, new object[] { newObject });
            this.Dispose();
            return newSingleInstance;
        }
    }

    // while a dictionary-based S, R system could be implemented via ViewOnSingleInstancePerApp<T> (T would be the dictionary),
    // we want to have stateless R nodes which work without the need to handle the lifetime of a handle on the dictionary.
    // with this class only the sender is stateful and disposes its channel, without the need of disposing the handle on the dictionary itself
    // when all senders dispose their channel there is no need for getting rid of the dictionary.
    // the dictionary survives a restart of the app (F8, F5). the channels don't.
    // further work has been put into the idea of being descriptive. when we have conflicting senders; 
    // after the confict is gone (only one sneder left on the channel) the remaining sender shall be able to be recieved
    public class Sender<TChannel, TValue> : IDisposable
    {
        static ConcurrentDictionary<TChannel, ChannelResolver> GetChannels()
            =>
            // let's fetch the one dictionary per app. It gets created once per session per dict type
            AppHost.Current.Services.GetOrAddService(_ => new ConcurrentDictionary<TChannel, ChannelResolver>());

        // all channels (per TChannel) within an app share this dictionary instance
        ConcurrentDictionary<TChannel, ChannelResolver> ChannelResolversPerChannel;

        // a sender resolver responsible for this particular channel
        ChannelResolver Resolver;

        readonly NodeContext NodeContext;

        public readonly TChannel Channel;

        TValue value;
        public TValue Value
        {
            get => value;
            set
            {
                this.value = value;
                if (!Active)
                    IVLRuntime.Current?.AddMessage(NodeContext.Path.Stack.First(), "Some other sender is already sending on the same channel.");
            }
        }

        public bool Active => Resolver.CurrentSender == this;

        public Sender(NodeContext nodeContext, 
            TChannel channel, TValue intialValue)
        {
            NodeContext = nodeContext;
            Channel = channel;

            // fetch something meaningful for this particular app. It gets created once per session and per (TChannels, TValue) type
            ChannelResolversPerChannel = GetChannels();

            // fetch the channel resolver for this channel. It is the one to talk to when its about that particular channel
            Resolver = ChannelResolversPerChannel.GetOrAdd(channel, _ => new ChannelResolver(channel, remover: () => ChannelResolversPerChannel.TryRemove(channel, out var _)));

            // promote our sender as the one responsible for this channel (we loose only if another sender sends on this channel already)
            Resolver.Promote(this);

            Value = intialValue;
        }

        // the whole point of the approach is to be able to make this method static
        public static void Receive(NodeContext nodeContext,
            TChannel channel, out TValue value, out bool success, bool warn = true)
        {
            if (success = GetChannels().TryGetValue(channel, out var resolver))
            {
                var sender = resolver.CurrentSender;
                if (sender != null)
                {
                    value = sender.Value;
                    return;
                }
            }
            else
            if (warn)
                {
                    // per (stateless) reciever node we want one warning sticking around, not one per frame. 
                    // so let's do the following once per node context path (equality is properly overidden..)
                    ResourceProvider.NewPooledSystemWide(nodeContext.Path,
                        _ =>
                        {
                            var messages = new CompositeDisposable();
                            var stack = nodeContext.Path.Stack.Pop(out var id);
                            IVLRuntime.Current?.AddPersistentMessage(new Message(id, MessageSeverity.Warning, "Didn't find sender for a moment. Make sure the sender gets executed first."))
                                .DisposeBy(messages);
                            while (!stack.IsEmpty)
                            {
                                stack = stack.Pop(out id);
                                IVLRuntime.Current?.AddPersistentMessage(new Message(id, MessageSeverity.Warning, $"A receiver inside this patch was unable to connect to sender \"{channel}\"."))
                                    .DisposeBy(messages);
                            }
                            return messages;
                        }, delayDisposalInMilliseconds: 3000)
                        .GetHandle()
                        .Dispose(); // messages will stick for some seconds
                }
                value = default;
        }

        public void Dispose()
        {
            // after removing this sender another sender might take over
            Resolver.Remove(this);
        }

        /// <summary>
        /// manages many senders on the same channel. first Sender wins. If the currently sending sender gets deleted another might jump in
        /// </summary>
        class ChannelResolver
        {
            List<Sender<TChannel, TValue>> SendersOnThisChannel = new List<Sender<TChannel, TValue>>();
            public Sender<TChannel, TValue> CurrentSender;
            TChannel Channel;
            Action Remover;

            public ChannelResolver(TChannel channel, Action remover)
            {
                Channel = channel;
                this.Remover = remover;
            }

            public void Promote(Sender<TChannel, TValue> sender)
            {
                SendersOnThisChannel.Add(sender);
                RefetchSender();
            }

            public void Remove(Sender<TChannel, TValue> sender)
            {
                SendersOnThisChannel.Remove(sender);
                RefetchSender();
            }

            void RefetchSender()
            {
                CurrentSender = SendersOnThisChannel.FirstOrDefault();
                if (CurrentSender == null)
                    Remover();
            }
        }
    }



    public static class StaticAnyType<T>
    {
        public static T Singleton;
    }

    public static class StaticPatches<T>
    {
        //[ThreadStatic] currently cannot load ThreadStatic into VL (Collectible type 'VL.Lib.Experimental.StaticNodes`1[VVVV.Experimental.MyManager]' may not have Thread or Context static members.)
        public static T StaticField;

        static StaticPatches()
        {
            StaticField = (T)AppHost.Current.CreateInstance(typeof(T), NodeContext.Default);
        }

        public static T Singleton
        {
            get
            {
                return StaticField;
            }
            set
            {
                StaticField = value;
            }
        }
    }

    public static class StaticClasses<T>
        where T : new()
    {
        //[ThreadStatic] currently cannot load ThreadStatic into VL (Collectible type 'VL.Lib.Experimental.StaticNodes`1[VVVV.Experimental.MyManager]' may not have Thread or Context static members.)
        public static T StaticField = new T();

        public static T Singleton
        {
            get
            {
                return StaticField;
            }
            set
            {
                StaticField = value;
            }
        }
    }
}
