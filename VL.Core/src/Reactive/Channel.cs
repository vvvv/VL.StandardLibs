using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using VL.Core;

#nullable enable

namespace VL.Lib.Reactive
{
    [Monadic(typeof(Monadic.ChannelFactory<>))]

    public interface IChannel<T> : ISubject<T?>, IDisposable
    {
        public T? Value { get; set; }
        Type ClrTypeOfValues { get; }
        ICollection Components { get; }
        TComponent? TryGetComponent<TComponent>() where TComponent : class;
        TComponent AddOrGetComponent<TComponent>(Func<TComponent> component) where TComponent : class;
        IChannel<object> ChannelOfObject { get; }
        bool Enabled { get; set; }
        object? Object { get => Value; set => Value = (T?)value; }
        bool IsBusy { get; }
    }

    internal abstract class C<T> : IChannel<T>, ISwappableGenericType
    {
        protected readonly Subject<T?> subject = new();
        List<object> components = new();

        object ISwappableGenericType.Swap(Type newType, Swapper swapObject)
        {
            var arg = newType.GenericTypeArguments[0];
            var channel = ChannelHelpers.CreateChannelOfType(arg);
            if (channel is not null)
                channel.Object = swapObject(Value, arg);
#nullable disable
            return channel;
#nullable enable
        }

        const int maxStack = 1;
        int stack;

        public bool IsBusy => stack > 0;

        T? value = default;
        public T? Value
        {
            get
            {
                return value;
            }
            set
            {
                if (!Enabled || !this.IsValid())
                    return;

                this.value = value;

                if (stack < maxStack)
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
        }

        IChannel<object> IChannel<T>.ChannelOfObject => channelOfObject;

        public Type ClrTypeOfValues => typeof(T);

        public ICollection Components => components;


        protected abstract IChannel<object> channelOfObject {get;}

        void IObserver<T?>.OnCompleted()
        {
            subject.OnCompleted();
        }

        void IObserver<T?>.OnError(Exception error)
        {
            subject.OnError(error);
        }

        void IObserver<T?>.OnNext(T? value) => subject.OnNext(value);

        IDisposable IObservable<T?>.Subscribe(IObserver<T?> observer) => subject.Subscribe(observer);

        public bool Enabled { get; set; } = true;

        void IDisposable.Dispose()
        {
            Enabled = false;
            subject.Dispose();
        }

        public TComponent? TryGetComponent<TComponent>() where TComponent: class
            => components.OfType<TComponent>().FirstOrDefault();

        public TComponent AddOrGetComponent<TComponent>(Func<TComponent> producer) where TComponent : class
        {
            var c = TryGetComponent<TComponent>();
            if (c is null)
                c = producer();
            components.Add(c);
            return c;
        }
    }


    internal class Channel<T> : C<T>, IChannel<object>
    {
        protected override IChannel<object> channelOfObject => this;

        IChannel<object> IChannel<object>.ChannelOfObject => this;

        object? IChannel<object>.Value { get => Value; set { Value = (T?)value; } }
        
        void IObserver<object?>.OnCompleted()
        {
            subject.OnCompleted();
        }

        void IObserver<object?>.OnError(Exception error)
        {
            subject.OnError(error);
        }

        void IObserver<object?>.OnNext(object? value) => subject.OnNext((T?)value); 
        
        IDisposable IObservable<object?>.Subscribe(IObserver<object?> observer)
        {
            if (observer is IObserver<T?> obsT)
                return subject.Subscribe(obsT);
            return subject.Subscribe(v => observer.OnNext(v), e => observer.OnError(e), () => observer.OnCompleted());
        }

        public static implicit operator T?(Channel<T> c) => c.Value;
    }


    public static class DummyChannelHelpers<T>
    {
        public static readonly IChannel<T> Instance; 
        
        static DummyChannelHelpers()
        {
            Instance = new DummyChannel<T>();
            Instance.Value = TypeUtils.Default<T>();
            Instance.Enabled = false;
        }
    }

    internal sealed class DummyChannel<T> : Channel<T>
    {
    }

    public static class ChannelHelpers
    {
        public static IChannel<IReadOnlyCollection<Attribute>> Attributes<T>(this IChannel<T> c)
            => c.AddOrGetComponent(() =>
                {
                    var c = CreateChannelOfType<IReadOnlyCollection<Attribute>>();
                    c.Value = Array.Empty<Attribute>();
                    return c;
                });

        public static IChannel<T> CreateChannelOfType<T>()
        {
            return new Channel<T>();
        }
        public static IChannel<object>? CreateChannelOfType(Type typeOfValues)
        {
            return Activator.CreateInstance(typeof(Channel<>).MakeGenericType(typeOfValues)) as IChannel<object>;
        }
        public static IChannel<object>? CreateChannelOfType(IVLTypeInfo typeOfValues)
        {
            return Activator.CreateInstance(typeof(Channel<>).MakeGenericType(typeOfValues.ClrType)) as IChannel<object>;
        }

        public static bool IsValid<T>([NotNullWhen(true)] this IChannel<T> c)
            => !(c is DummyChannel<T>);

        public static void EnsureValue<T>(this IChannel<T> input, T? value, bool force = false)
        {
            if (force || !EqualityComparer<T>.Default.Equals(input.Value, value))
                input.Value = value;
        }

        // not really necessary
        public static void EnsureValue(this IChannel<object> input, object value, bool force = false)
        {
            if (force || !Equals(input.Object, value))
                input.Object = value;
        }

        public static IDisposable Merge<T>(this IChannel<T> a, IChannel<T> b, ChannelMergeInitialization initialization, ChannelSelection pushEagerlyTo)
        {
            return Merge(a, b, v => v, v => v, initialization, pushEagerlyTo);
        }

        public static IDisposable Merge<A, B>(this IChannel<A> a, IChannel<B> b, Func<A?, B?> toB, Func<B?, A?> toA, ChannelMergeInitialization initialization, ChannelSelection pushEagerlyTo)
        {
            var subscription = new CompositeDisposable();

            switch (initialization)
            {
                case ChannelMergeInitialization.UseA:
                    b.EnsureValue(toB(a.Value));
                    break;
                case ChannelMergeInitialization.UseB:
                    a.EnsureValue(toA(b.Value));
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
                        b.EnsureValue(toB(v), pushEagerlyTo.HasFlag(ChannelSelection.ChannelB));
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
                        a.EnsureValue(toA(v), pushEagerlyTo.HasFlag(ChannelSelection.ChannelA));
                    }
                    finally
                    {
                        isBusy = false;
                    }
                }
            }));

            return subscription;
        }

        public static IDisposable Merge<A, B>(this IChannel<A> a, IChannel<B> b, Func<A?, Optional<B>> toB, Func<B?, Optional<A>> toA, ChannelMergeInitialization initialization, ChannelSelection pushEagerlyTo)
        {
            var subscription = new CompositeDisposable();

            switch (initialization)
            {
                case ChannelMergeInitialization.UseA:
                {
                    var optionalV = toB(a.Value);
                    if (optionalV.HasValue)
                        b.EnsureValue(optionalV.Value);
                    break;
                }
                case ChannelMergeInitialization.UseB:
                {
                    var optionalV = toA(b.Value);
                    if (optionalV.HasValue)
                        a.EnsureValue(optionalV.Value);
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
                            b.EnsureValue(x.Value, pushEagerlyTo.HasFlag(ChannelSelection.ChannelB));
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
                            a.EnsureValue(x.Value, pushEagerlyTo.HasFlag(ChannelSelection.ChannelA));
                    }
                    finally
                    {
                        isBusy = false;
                    }
                }
            }));

            return subscription;
        }


        // not really necessary
        //public static IDisposable Merge(this IChannel<object> a, IChannel<object> b, ChannelMergeInitialization initialization, ChannelSelection pushEagerlyTo)
        //{
        //    return Merge(a, b, v => v, v => v, initialization, pushEagerlyTo);
        //}

        //// not really necessary
        //public static IDisposable Merge(this IChannel<object> a, IChannel<object> b, Func<object, object> toB, Func<object, object> toA, ChannelMergeInitialization initialization, ChannelSelection pushEagerlyTo)
        //    => a.ChannelOfObject.Merge(b.ChannelOfObject, toB, toA, initialization, pushEagerlyTo);
    }

}
#nullable disable
