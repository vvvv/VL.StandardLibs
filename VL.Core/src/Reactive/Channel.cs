using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using VL.Core;
using VL.Core.CompilerServices;

namespace VL.Lib.Reactive
{
    public abstract class Channel : IDisposable
    {
        public abstract object Object { get; set; }
        public abstract Channel<object> ChannelOfObject { get; }
        public abstract Channel<IReadOnlyCollection<Attribute>> Attributes { get; }

        public ICollection Components => components;

        List<object> components = new List<object>();

        public IObservable<object> ToObservableOfObject()
        {
            if (this is IObservable<object> x)
                return x;
            return ChannelOfObject;
        }

        public abstract Type ClrTypeOfValues { get; } //ivltypeinfo would be nice. but you need scope.

        public static Channel CreateChannelOfType(Type typeOfValues)
        {
            return Activator.CreateInstance(typeof(Channel<>).MakeGenericType(typeOfValues)) as Channel;
        }
        public static Channel CreateChannelOfType(IVLTypeInfo typeOfValues)
        {
            return Activator.CreateInstance(typeof(Channel<>).MakeGenericType(typeOfValues.ClrType)) as Channel;
        }

        public T TryGetComponent<T>() => components.OfType<T>().FirstOrDefault();

        public abstract void Dispose();

        internal bool IsDummy;
    }

    [Monadic(typeof(Monadic.ChannelFactory<>))]
    public class Channel<T> : Channel, ISubject<T>, ISwappableGenericType
    {
        readonly Subject<T> Subject = new Subject<T>();

        public Channel()
        {
            FChannelOfObject = new Lazy<Channel<object>>(() =>
            {
                var c = new Channel<object>() { Value = Value };
                this.Merge(c, a => a, b => (T)b, ChannelMergeInitialization.None, pushEagerlyTo: ChannelSelection.Both);
                return c;
            });
            FAttributes = new Lazy<Channel<IReadOnlyCollection<Attribute>>>(() =>
            {
                return new Channel<IReadOnlyCollection<Attribute>>()
                {
                    Value = Array.Empty<Attribute>()
                };
            });
        }

        public override void Dispose()
        {
            Enabled = false;
            Subject.Dispose();
            if (FChannelOfObject.IsValueCreated)
                ChannelOfObject.Dispose();
            if (FAttributes.IsValueCreated)
                Attributes.Dispose();
        }

        void IObserver<T>.OnCompleted()
        {
            Subject.OnCompleted();
        }

        void IObserver<T>.OnError(Exception error)
        {
            Subject.OnError(error);
        }

        void IObserver<T>.OnNext(T value)
        {
            Value = value;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return Subject.Subscribe(observer);
        }

        object ISwappableGenericType.Swap(Type newType, Swapper swapObject)
        {
            var arg = newType.GenericTypeArguments[0];
            var channel = CreateChannelOfType(arg);
            channel.Object = swapObject(Object, arg);
            return channel;
        }

        const int maxStack = 1;
        int stack;

        public bool IsBusy => stack > 0;

        T value = default;
        public T Value 
        { 
            get
            {
                return value;
            }
            set
            {
                if (!Enabled || IsDummy)
                    return;

                this.value = value;

                if (stack < maxStack)
                {
                    stack++;
                    try
                    {
                        Subject.OnNext(value);
                    }
                    finally
                    {
                        stack--;
                    }
                }
            } 
        }

        public override object Object { get => Value; set => Value = (T)value; }

        Lazy<Channel<object>> FChannelOfObject;
        Lazy<Channel<IReadOnlyCollection<Attribute>>> FAttributes;

        public override Channel<object> ChannelOfObject => FChannelOfObject.Value;
        public override Channel<IReadOnlyCollection<Attribute>> Attributes => FAttributes.Value;

        public override Type ClrTypeOfValues => typeof(T);

        public bool Enabled { get; set; } = true;

        public static implicit operator T(Channel<T> c) => c.Value;
    }

    public sealed class DummyChannel<T> : Channel<T>
    {
        public static readonly Channel<T> Instance;

        static DummyChannel()
        {
            Instance = new DummyChannel<T>();
            Instance.Value = TypeUtils.Default<T>();
            Instance.Enabled = false;
            Instance.IsDummy = true;
        }
    }

    public static class ChannelHelpers
    {
        public static bool IsValid([NotNullWhen(true)] this Channel c)
            => c != null && !c.IsDummy;

        public static void EnsureValue<T>(this Channel<T> input, T value, bool force = false)
        {
            if (force || !EqualityComparer<T>.Default.Equals(input.Value, value))
                input.Value = value;
        }

        public static void EnsureValue(this Channel input, object value, bool force = false)
        {
            if (force || !Equals(input.Object, value))
                input.Object = value;
        }

        public static IDisposable Merge<T>(this Channel<T> a, Channel<T> b, ChannelMergeInitialization initialization, ChannelSelection pushEagerlyTo)
        {
            return Merge(a, b, v => v, v => v, initialization, pushEagerlyTo);
        }

        public static IDisposable Merge<A, B>(this Channel<A> a, Channel<B> b, Func<A, B> toB, Func<B, A> toA, ChannelMergeInitialization initialization, ChannelSelection pushEagerlyTo)
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

        public static IDisposable Merge<A, B>(this Channel<A> a, Channel<B> b, Func<A, Optional<B>> toB, Func<B, Optional<B>> toA, ChannelMergeInitialization initialization, ChannelSelection pushEagerlyTo)
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

        public static IDisposable Merge(this Channel a, Channel b, ChannelMergeInitialization initialization, ChannelSelection pushEagerlyTo)
        {
            return Merge(a, b, v => v, v => v, initialization, pushEagerlyTo);
        }

        public static IDisposable Merge(this Channel a, Channel b, Func<object, object> toB, Func<object, object> toA, ChannelMergeInitialization initialization, ChannelSelection pushEagerlyTo)
        {
            var subscription = new CompositeDisposable();

            switch (initialization)
            {
                case ChannelMergeInitialization.UseA:
                    b.EnsureValue(toB(a.Object));
                    break;
                case ChannelMergeInitialization.UseB:
                    a.EnsureValue(toA(b.Object));
                    break;
            }

            var isBusy = false;
            subscription.Add(a.ToObservableOfObject().Subscribe(v =>
            {
                if (!isBusy)
                {
                    isBusy = true;
                    try
                    {
                        b.EnsureValue(toB(a.Object), pushEagerlyTo.HasFlag(ChannelSelection.ChannelB));
                    }
                    finally
                    {
                        isBusy = false;
                    }
                }
            }));
            subscription.Add(b.ToObservableOfObject().Subscribe(v =>
            {
                if (!isBusy)
                {
                    isBusy = true;
                    try
                    {
                        a.EnsureValue(toA(b.Object), pushEagerlyTo.HasFlag(ChannelSelection.ChannelA));
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
}
