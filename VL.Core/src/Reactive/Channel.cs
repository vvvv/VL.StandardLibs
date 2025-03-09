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

        public override string ToString() => Path != null && !Path.StartsWith("_Anonymous") ? $"{Path} = {Value}" : $"{Value}";

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


}
#nullable disable
