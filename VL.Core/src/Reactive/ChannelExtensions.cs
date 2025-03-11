using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using VL.Core;
using VL.Core.Reactive;
using VL.Lib.Basics.Resources;
using VL.Lib.Collections;
using VL.Lib.Reactive;

namespace VL.Lib.Reactive
{
    public static partial class ChannelHelpers
    {
        public static IChannel<T> CreateChannelOfType<T>()
        {
            return new Channel<T>();
        }
        public static IChannel<object> CreateChannelOfType(Type typeOfValues)
        {
            return (IChannel<object>)Activator.CreateInstance(typeof(Channel<>).MakeGenericType(typeOfValues))!;
        }
        public static IChannel<object> CreateChannelOfType(IVLTypeInfo typeOfValues)
        {
            return (IChannel<object>)Activator.CreateInstance(typeof(Channel<>).MakeGenericType(typeOfValues.ClrType))!;
        }






        public static void EnsureValue<T>(this IChannel<T> input, T? value, bool force = false, string? author = default)
        {
            if (force || !EqualityComparer<T>.Default.Equals(input.Value, value))
                input.SetValueAndAuthor(value, author);
        }

        public static void EnsureObject(this IChannel input, object? value, bool force = false, string? author = default)
        {
            if (force || !EqualityComparer<object>.Default.Equals(input.Object, value))
                input.SetObjectAndAuthor(value, author);
        }








        public static IDisposable Merge<T>(this IChannel<T> a, IChannel<T> b,
            ChannelMergeInitialization initialization, ChannelSelection pushEagerlyTo)
        {
            return Merge(a, b, v => v, v => v, initialization, pushEagerlyTo);
        }

        public static IDisposable Merge<A, B>(this IChannel<A> a, IChannel<B> b, Func<A?, B?> toB, Func<B?, A?> toA,
            ChannelMergeInitialization initialization, ChannelSelection pushEagerlyTo)
        {
            if (!a.IsValid() || !b.IsValid())
                return Disposable.Empty;

            var subscription = new CompositeDisposable();

            switch (initialization)
            {
                case ChannelMergeInitialization.UseA:
                    b.EnsureValue(toB(a.Value), author: a.LatestAuthor);
                    break;
                case ChannelMergeInitialization.UseB:
                    a.EnsureValue(toA(b.Value), author: b.LatestAuthor);
                    break;
            }

            var isBusy = false;
            subscription.Add(a.Subscribe(v =>
            {
                if (!isBusy)
                {
                    isBusy = true;
                    try
                    {
                        b.EnsureValue(toB(v), pushEagerlyTo.HasFlag(ChannelSelection.ChannelB), a.LatestAuthor);
                    }
                    finally
                    {
                        isBusy = false;
                    }
                }
            }));
            subscription.Add(b.Subscribe(v =>
            {
                if (!isBusy)
                {
                    isBusy = true;
                    try
                    {
                        a.EnsureValue(toA(v), pushEagerlyTo.HasFlag(ChannelSelection.ChannelA), b.LatestAuthor);
                    }
                    finally
                    {
                        isBusy = false;
                    }
                }
            }));

            return subscription;
        }

        public static IDisposable Merge<A, B>(this IChannel<A> a, IChannel<B> b, Func<A?, Optional<B>> toB, Func<B?, Optional<A>> toA,
            ChannelMergeInitialization initialization, ChannelSelection pushEagerlyTo)
        {
            if (!a.IsValid() || !b.IsValid())
                return Disposable.Empty;

            var subscription = new CompositeDisposable();

            switch (initialization)
            {
                case ChannelMergeInitialization.UseA:
                    {
                        var optionalV = toB(a.Value);
                        if (optionalV.HasValue)
                            b.EnsureValue(optionalV.Value, author: a.LatestAuthor);
                        break;
                    }
                case ChannelMergeInitialization.UseB:
                    {
                        var optionalV = toA(b.Value);
                        if (optionalV.HasValue)
                            a.EnsureValue(optionalV.Value, author: b.LatestAuthor);
                        break;
                    }
            }

            var isBusy = false;
            subscription.Add(a.Subscribe(v =>
            {
                if (!isBusy)
                {
                    isBusy = true;
                    try
                    {
                        var x = toB(v);
                        if (x.HasValue)
                            b.EnsureValue(x.Value, pushEagerlyTo.HasFlag(ChannelSelection.ChannelB), a.LatestAuthor);
                    }
                    finally
                    {
                        isBusy = false;
                    }
                }
            }));
            subscription.Add(b.Subscribe(v =>
            {
                if (!isBusy)
                {
                    isBusy = true;
                    try
                    {
                        var x = toA(v);
                        if (x.HasValue)
                            a.EnsureValue(x.Value, pushEagerlyTo.HasFlag(ChannelSelection.ChannelA), b.LatestAuthor);
                    }
                    finally
                    {
                        isBusy = false;
                    }
                }
            }));

            return subscription;
        }


        record class UpLink(IDisposable Disposable) : IDisposable
        {
            public void Dispose()
            {
                Disposable.Dispose();
            }
        }


        public static void InitSubChannel(this IChannel channel, ObjectGraphNode node)
        {
            if (node.AccessedViaKey is IVLPropertyInfo p)
            {
                var attribChannel = Attributes(channel);
                if (!attribChannel.Value.SequenceEqual(p.Attributes))
                {
                    attribChannel.SetValueAndAuthor(p.Attributes, author: "InitSubChannel");
                }
            }
        }


        public static void GetOrAddSubChannel<A, B>(this IChannel<A> main, string relativePath, out IChannel<B> subChannel, out IDisposable handleOnSomeBridges)
            where A : class
        {
            subChannel = null;
            handleOnSomeBridges = Disposable.Empty;

            static IEnumerable<ObjectGraphNode> yieldPathToNode(ObjectGraphNode node)
            {
                if (node.Parent != null)
                    foreach (var n in yieldPathToNode(node.Parent))
                        yield return n;
                yield return node;
            }

            if (main == null)
                throw new ArgumentException("Main channel must be set.", nameof(main));

            if (relativePath.Length == 0)
                throw new ArgumentException("Relative path must not be empty.", nameof(relativePath));

            if (string.IsNullOrWhiteSpace(main.Path))
                throw new ArgumentException("Select(ByPath) only supported on shared channels.", nameof(main));

            var node = main.Object.TryGetObjectGraphNodeByPath(relativePath);

            if (node.HasValue)
            {
                var channelHub = AppHost.Current.Services.GetRequiredService<IChannelHub>();
                IChannel<object> parent = null;
                IChannel<object> channel = null;
                var handleOnSomeSyncs = new CompositeDisposable();
                foreach (var n in yieldPathToNode(node.Value))
                {
                    if (parent == null)
                    {
                        parent = main as IChannel<object>;
                        continue;
                    }

                    var globalPath = main.Path + n.Path;
                    channel = channelHub.TryGetChannel(globalPath);
                    if (channel == null)
                    {
                        channel = channelHub.TryAddChannel(globalPath, typeof(object));
                        InitSubChannel(channel, n);
                    }

                    var subSync = channel.EnsureSingleComponentOfType(() =>
                    {
                        return
                            ResourceProvider.New(() => SubChannelSyncer(parent, channel, n.AccessedViaKeyPath))
                            .ShareInParallel();
                    });

                    handleOnSomeSyncs.Add(subSync.GetHandle()); // diposing this will keep the channels but remove the sync (if not needed by other SelectByPath)
                    parent = channel;
                }
                subChannel = Channel.Create<B>(default); //TODO: view
                handleOnSomeSyncs.Add(channel.Merge(subChannel, v => v is B ? (B)v : default, v => v, ChannelMergeInitialization.UseA, ChannelSelection.Both));
                handleOnSomeBridges = handleOnSomeSyncs;
            }
        }


        private static IDisposable SubChannelSyncer<A, B>(IChannel<A> main, IChannel<B> sub, string relativePath)
            where A : class
        {
            if (!main.IsValid() || !sub.IsValid())
                return Disposable.Empty;

            var subscription = new CompositeDisposable();

            if (main.Value.TryGetValueByPath(relativePath, default, out B value, out _))
                sub.EnsureValue(value, author: "SubChannelSyncer.Init");

            var isBusy = false;
            subscription.Add(main.Subscribe(v =>
            {
                if (!isBusy)
                {
                    isBusy = true;
                    try
                    {
                        if (main.Value.TryGetValueByPath(relativePath, default, out B value, out _))
                            sub.EnsureValue(value, author: "SubChannelSyncer.FromParent");
                    }
                    finally
                    {
                        isBusy = false;
                    }
                }
            }));
            subscription.Add(sub.Subscribe(v =>
            {
                if (!isBusy)
                {
                    isBusy = true;
                    try
                    {
                        var newBig = main.Value.WithValueByPath(relativePath, v, out _);
                        main.SetValueAndAuthor(newBig, author: "SubChannelSyncer.FromChild");
                    }
                    finally
                    {
                        isBusy = false;
                    }
                }
            }));

            return subscription;
        }


        public static void AddComponent(this IChannel channel, object component)
        {
            channel.Components = channel.Components.Add(component);
        }

        public static void RemoveComponent(this IChannel channel, object component)
        {
            channel.Components = channel.Components.Remove(component);
            //(component as IDisposable)?.Dispose();
        }

        public static TComponent? TryGetComponent<TComponent>(this IChannel channel) where TComponent : class
        {
            foreach (var c in channel.Components)
                if (c is TComponent t)
                    return t;
            return default;
        }

        public static TComponent EnsureSingleComponentOfType<TComponent>(this IChannel channel, Func<TComponent> producer, bool renew = false) where TComponent : class
        {
            var c = channel.TryGetComponent<TComponent>();
            if (c is null)
            {
                c = producer();
                channel.Components = channel.Components.Add(c);
                return c;
            }

            if (!renew)
                return c;

            var newC = producer();
            channel.Components = channel.Components.Replace(c, newC);
            (c as IDisposable)?.Dispose();
            return newC;
        }

        public static object EnsureSingleComponentOfType(this IChannel channel, object component, bool renew)
        {
            var type = component.GetType();
            foreach (object? c in channel.Components)
            {
                if (c.GetType() == type)
                {
                    if (!renew)
                    {
                        (component as IDisposable)?.Dispose();
                        return c;
                    }
                    channel.Components = channel.Components.Replace(c, component);
                    (c as IDisposable)?.Dispose();
                    return component;
                }
            }
            channel.Components = channel.Components.Add(component);
            return component;
        }

        public static IChannel<Spread<Attribute>> Attributes(this IChannel channel)
        {
            return channel.TryGetComponent<IChannel<Spread<Attribute>>>() ?? channel.EnsureSingleComponentOfType(() => Channel.Create(Spread<Attribute>.Empty), false);
        }

        public static bool TryGetAttribute<T>(this IChannel channel, [NotNullWhen(true)] out T? attribute) where T : Attribute
        {
            var attributes = channel.TryGetComponent<IChannel<Spread<Attribute>>>()?.Value;

            if (attributes is not null)
            {
                foreach (var a in attributes)
                {
                    if (a is T t)
                    {
                        attribute = t;
                        return true;
                    }
                }
            }

            attribute = null;
            return false;
        }

        public static TAttribute? GetAttribute<TAttribute>(this IChannel channel)
            where TAttribute : Attribute
        {
            if (channel.TryGetAttribute(out TAttribute? result))
                return result;
            return default;
        }

    }
}
