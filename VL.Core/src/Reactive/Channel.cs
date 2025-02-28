using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using VL.Core;
using VL.Core.EditorAttributes;
using VL.Lib.Collections;

#nullable enable

namespace VL.Lib.Reactive
{
    public interface IChannel : IHasAttributes, IDisposable
    {
        Type ClrTypeOfValues { get; }
        ImmutableArray<object> Components { get; set; }
        IChannel<object> ChannelOfObject { get; }
        bool Enabled { get; set; }
        bool IsBusy { get; }
        object? Object { get; set; }
        string? LatestAuthor { get; }
        void SetObjectAndAuthor(object? @object, string? author);
        IDisposable BeginChange();
        string? Path { get; }
        internal int Revision { get; }
    }

    [MonadicTypeFilter(typeof(ChannelMonadicTypeFilter))]
    public interface IChannel<T> : IChannel, ISubject<T?>, IMonadicValue<T>
    {
        static IMonadicValue<T> IMonadicValue<T>.Create(NodeContext nodeContext, T? value) => Channel.Create(value!);
        static bool IMonadicValue<T>.HasCustomDefault => true; // We use DummyChannel as default
        static IMonadicValue<T> IMonadicValue<T>.Default => DummyChannel<T>.Instance;
        new T? Value { get; set; }
        void SetValueAndAuthor(T? value, string? author);
        Func<T?, Optional<T?>>? Validator { set; }
    }

    internal abstract class C<T> : IChannel<T>, IMonadicValue<T>
    {
        protected readonly Subject<T?> subject = new();
        protected int lockCount = 0;
        protected int revision = 0;
        protected int revisionOnLockTaken = 0;

        private IChannel<Spread<Attribute>>? attributesChannel;
        private TagsCache? tagsCache;

        public ImmutableArray<object> Components { get; set; } = ImmutableArray<object>.Empty;

        const int maxStack = 1;
        int stack;

        public bool IsBusy => stack > 0;

        protected T? value = default;
        public T? Value
        {
            get
            {
                return value;
            }
            set
            {
                SetValueAndAuthor(value, null);
            }
        }

        public Func<T?, Optional<T?>>? Validator { private get; set; } = null;

        public void SetValueAndAuthor(T? value, string? author)
        {
            AssertAlive();
            if (!Enabled || !this.IsValid())
                return;

            if (Validator != null)
            {
                var x = Validator(value);
                if (x.HasNoValue)
                    return;
                value = x.Value;
            }

            LatestAuthor = author;
            this.value = value;
            revision++;

            if (stack < maxStack && lockCount == 0)
            {
                stack++;
                try
                {
                    subject.OnNext(value);
                }
                finally
                {
                    stack--;
                }
            }
        }

        object? IChannel.Object { get => Value; set => Value = (T?)value; }

        int IChannel.Revision => revision;

        void IChannel.SetObjectAndAuthor(object? @object, string? author)
        {
            SetValueAndAuthor((T?)@object, author);
        }

        IChannel<object> IChannel.ChannelOfObject => channelOfObject;

        public bool HasValue => true;

        public virtual bool AcceptsValue => true;

        public Type ClrTypeOfValues => typeof(T);


        protected abstract IChannel<object> channelOfObject {get;}

        void IObserver<T?>.OnCompleted()
        {
            AssertAlive();
            if (Enabled)
                subject.OnCompleted();
        }

        void IObserver<T?>.OnError(Exception error)
        {
            AssertAlive();
            if (Enabled)
                subject.OnError(error);
        }

        void IObserver<T?>.OnNext(T? value)
        {
            AssertAlive();
            SetValueAndAuthor(value, null);
        }

        IDisposable IObservable<T?>.Subscribe(IObserver<T?> observer)
        {
            AssertAlive();
            if (subject.IsDisposed) 
                return Disposable.Empty;                
            return subject.Subscribe(observer);
        }

        [Conditional("DEBUG")]
        protected void AssertAlive()
        {
            // Debug.Assert causes complete crash in DEBUG builds without a debugger attached
            //Debug.Assert(!subject.IsDisposed, "you work with a disposed channel!");
        }

        public bool Enabled { get; set; } = true;

        public string? LatestAuthor { get; set; }

        public string? Path { get; protected set; }

        bool disposing = false;
        void IDisposable.Dispose()
        {
            if (disposing)
                return;

            disposing = true;
            try
            {
                Enabled = false;
                foreach (var c in Components)
                    (c as IDisposable)?.Dispose();
                Components = ImmutableArray<object>.Empty;
                subject.Dispose();
            }
            finally
            {
                disposing = false;
            }
        }

        public IDisposable BeginChange()
        {
            if (lockCount == 0)
                revisionOnLockTaken = revision;
            lockCount++;
            return Disposable.Create(EndChange);
        }

        void EndChange()
        {
            lockCount--;
            if (lockCount == 0 && revisionOnLockTaken != revision)
                SetValueAndAuthor(this.Value, LatestAuthor);
        }

        Spread<Attribute> IHasAttributes.Attributes => GetAttributesChannel().Value ?? Spread<Attribute>.Empty;

        Spread<string> IHasAttributes.Tags => (tagsCache ??= new(GetAttributesChannel())).Tags;

        IChannel<Spread<Attribute>> GetAttributesChannel() => attributesChannel ??= this.Attributes();

        T? IMonadicValue<T>.Value => Value;

        IMonadicValue<T> IMonadicValue<T>.SetValue(T? value)
        {
            SetValueIfChanged(value);
            return this;
        }

        protected void SetValueIfChanged(T? value)
        {
            if (lastValue.HasNoValue || !EqualityComparer<T>.Default.Equals(value, lastValue.Value))
            {
                lastValue = value;
                Value = value;
            }
        }
        Optional<T?> lastValue;

        sealed class TagsCache
        {
            private IChannel<Spread<Attribute>> attributes;
            private Spread<string>? tags;
            private int revision;

            public TagsCache(IChannel<Spread<Attribute>> attributes)
            {
                this.attributes = attributes;
            }

            public Spread<string> Tags
            {
                get
                {
                    var r = revision;
                    if (Interlocked.Exchange(ref revision, attributes.Revision) != r)
                        tags = null;

                    if (tags is null)
                        tags = attributes.Value?.OfType<TagAttribute>().Select(t => t.TagLabel).ToSpread() ?? Spread<string>.Empty;

                    return tags;
                }
            }
        }
    }

    internal class Channel<T> : C<T>, IChannel<object>, IInternalChannel
    {
        object? IMonadicValue<object>.Value => Value;

        object? IMonadicValue.BoxedValue => Value;

        IMonadicValue<object> IMonadicValue<object>.SetValue(object? value)
        {
            SetValueIfChanged((T?)value);
            return this;
        }

        protected override IChannel<object> channelOfObject => this;

        object? IChannel<object>.Value { get => Value; set { Value = (T?)value; } }

        Func<object?, Optional<object?>>? IChannel<object>.Validator 
        {
            set
            {
                if (value == null)
                {
                    Validator = null;
                    return;
                }
                Validator = v =>
                {
                    var opt = value!.Invoke(v);
                    if (opt.HasValue)
                        return (T?)opt.Value;
                    return new Optional<T?>();
                };
            }
        }

        void IChannel<object>.SetValueAndAuthor(object? value, string? author)
        {
            SetValueAndAuthor((T?)value, author);
        }

        void IObserver<object?>.OnCompleted()
        {
            AssertAlive();
            if (Enabled)
                subject.OnCompleted();
        }

        void IObserver<object?>.OnError(Exception error)
        {
            AssertAlive();
            if (Enabled)
                subject.OnError(error);
        }

        void IObserver<object?>.OnNext(object? value)
        {
            Value = (T?)value;
        }
        
        IDisposable IObservable<object?>.Subscribe(IObserver<object?> observer)
        {
            AssertAlive();
            if (subject.IsDisposed)
                return Disposable.Empty;
            if (observer is IObserver<T?> obsT)
                return subject.Subscribe(obsT);
            return subject.Subscribe(v => observer.OnNext(v), e => observer.OnError(e), () => observer.OnCompleted());
        }

        public static implicit operator T?(Channel<T> c) => c.Value;

        public override string ToString() => Path != null ? $"{Path} = {Value}" : $"{Value}";

        void IInternalChannel.SetPath(string path)
        {
            Path = path;
        }
    }

    internal interface IInternalChannel
    {
        void SetPath(string path);
    }

    interface IDummyChannel { }

    internal sealed class DummyChannel<T> : Channel<T>, IDummyChannel
    {
        public static readonly IChannel<T> Instance = new DummyChannel<T>();

        public DummyChannel() 
            : this(default(T)) // We do not want the VL default, T could be the whole user application stored in an unmanaged static reference -> disaster
        {
        }

        public DummyChannel(T? value)
        {
            this.value = value;
            Enabled = false;
        }

        public override bool AcceptsValue => false;
    }

    public static class Channel
    {
        public static IChannel<T> Create<T>(T value)
        {
            var channel = ChannelHelpers.CreateChannelOfType<T>();
            channel.Value = value;
            return channel;
        }
    }

    public static class ChannelHelpers
    {
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

        public static TComponent EnsureSingleComponentOfType<TComponent>(this IChannel channel, Func<TComponent> producer, bool renew) where TComponent : class
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

        public static IChannel<T> Dummy<T>() => DummyChannel<T>.Instance;

        public static readonly IChannel<object> DummyNonGeneric = new DummyChannel<object>("ceci n'est pas une pipe");

        public static bool IsValid([NotNullWhen(true)] this IChannel? c)
            => c is not null && c is not IDummyChannel;

        [Obsolete("No longer needed", error: true)]
        public static bool IsSystemGenerated(this IChannel channel) => false;

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
    }

    sealed class ChannelMonadicTypeFilter : IMonadicTypeFilter
    {
        public bool Accepts(TypeDescriptor typeDescriptor)
        {
            if (typeDescriptor.ClrType == null)
                return true; // let's not restrict generic patches with explicit type arguments. Auto-lifting shall work with the idea that the type might be ok.

            return !typeDescriptor.ClrType.IsAssignableTo(typeof(IChannel)); // T shall not be a channel itself - at least not when auto lifting. explicit usage ok.
        }
    }

}
#nullable disable
