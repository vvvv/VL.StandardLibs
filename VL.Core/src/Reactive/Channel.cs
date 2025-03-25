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
using VL.Core.Reactive;
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
            if (!Enabled)
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
        public Channel()
        {
            Path = ChannelHubHelpers.CreateUniqueKey();
        }

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

        public override string ToString() => Path != null && !this.IsAnonymous() ? $"{Path} = {Value}" : $"{Value}";

        void IInternalChannel.SetPath(string path)
        {
            Path = path;
        }
    }

    internal interface IInternalChannel
    {
        void SetPath(string path);
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



    internal abstract class ChannelView_<T> : IChannel<T>
    {
        static T asT(object? value)
        {
            if (value is T t)
                return t;
            return default!;
        }

        protected readonly IChannel<object> original;

        public ChannelView_(IChannel original)
        {
            this.original = original.ChannelOfObject;
        }

        public T? Value
        {
            get => asT(original.Object);
            set => original.Object = value;
        }

        public Func<T?, Optional<T?>>? Validator
        {
            set
            {
                if (value == null)
                {
                    original.Validator = null;
                    return;
                }
                original.Validator = v =>
                {
                    var opt = value(asT(v));
                    if (opt.HasValue)
                        return opt.Value;
                    return new Optional<object?>();
                };
            }
        }

        public Type ClrTypeOfValues => typeof(T);

        public ImmutableArray<object> Components
        {
            get => original.Components;
            set => original.Components = value;
        }

        IChannel<object> IChannel.ChannelOfObject => channelOfObject;

        protected abstract IChannel<object> channelOfObject
        {
            get;
        }

        public bool Enabled
        {
            get => original.Enabled;
            set => original.Enabled = value;
        }

        public bool IsBusy => original.IsBusy;

        public object? Object
        {
            get => original.Object;
            set => original.Object = value;
        }

        public string? LatestAuthor => original.LatestAuthor;

        public string? Path => original.Path;

        public Spread<Attribute> Attributes => original.Attributes;

        public bool HasValue => original.HasValue;

        public bool AcceptsValue => original.AcceptsValue;

        int IChannel.Revision => original.Revision;

        public IDisposable BeginChange() => original.BeginChange();

        public void Dispose()
        {
            // nothing to be done.
            // we didn't subscribe to the original channel, so we don't need to unsubscribe.
        }

        public void OnCompleted() => original.OnCompleted();

        public void OnError(Exception error) => original.OnError(error);

        public void OnNext(T? value) => original.OnNext(value);

        public void SetObjectAndAuthor(object? @object, string? author) => original.SetObjectAndAuthor(@object, author);

        public IMonadicValue<T> SetValue(T? value)
        {
            throw new NotImplementedException();
        }

        public void SetValueAndAuthor(T? value, string? author) => original.SetValueAndAuthor(value, author);

        public IDisposable Subscribe(IObserver<T?> observer)
        {
            return original.Subscribe(new ObserverWrapper(observer));
        }

        private struct ObserverWrapper : IObserver<object?>
        {
            private readonly IObserver<T?> observer;

            public ObserverWrapper(IObserver<T?> observer)
            {
                this.observer = observer;
            }

            public void OnCompleted()
            {
                observer.OnCompleted();
            }

            public void OnError(Exception error)
            {
                observer.OnError(error);
            }

            public void OnNext(object? value)
            {
                observer.OnNext(asT(value));
            }
        }
    }


    internal class ChannelView<T> : ChannelView_<T>, IChannel, IChannel<object>
    {
        public ChannelView(IChannel original) : base(original)
        {
        }

        object? IChannel<object>.Value
        {
            get => original.Value;
            set => original.Value = value;
        }

        void IChannel<object>.SetValueAndAuthor(object? value, string? author)
        {
            original.SetValueAndAuthor(value, author);
        }

        Func<object?, Optional<object?>>? IChannel<object>.Validator
        {
            set => original.Validator = value;
        }

        void IObserver<object?>.OnNext(object? value)
        {
            original.OnNext(value);
        }

        IDisposable IObservable<object?>.Subscribe(IObserver<object?> observer)
        {
            return original.Subscribe(observer);
        }

        object? IMonadicValue<object>.Value => original.Value;

        IMonadicValue<object> IMonadicValue<object>.SetValue(object? value)
        {
            return ((IMonadicValue<object>)original).SetValue(value);
        }

        object? IMonadicValue.BoxedValue => original.BoxedValue;

        protected override IChannel<object> channelOfObject => this;

        public static implicit operator T?(ChannelView<T> c) => c.Value;

        public override string ToString() => original.ToString();

    }

}
#nullable disable
