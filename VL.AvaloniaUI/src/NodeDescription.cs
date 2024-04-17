using Avalonia;
using Avalonia.Collections;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VL.Core;
using VL.Core.Diagnostics;

namespace VL.AvaloniaUI
{
    internal static class NodeDescription
    {
        public static NodeDescription<T> New<T>(IVLNodeDescriptionFactory factory, Func<T> createInstance, Action<NodeDescription<T>> initializer)
            where T : AvaloniaObject
        {
            return new NodeDescription<T>(factory, createInstance, initializer);
        }
    }

    internal sealed class NodeDescription<T> : IVLNodeDescription, IInfo
        where T : AvaloniaObject
    {
        private readonly IVLNodeDescriptionFactory factory;
        private readonly Func<T> createInstance;
        private readonly List<PinDescription> inputs, outputs;

        public NodeDescription(IVLNodeDescriptionFactory factory, Func<T> createInstance, Action<NodeDescription<T>> initializer)
        {
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
            this.createInstance = createInstance ?? throw new ArgumentNullException(nameof(createInstance));

            this.inputs = new List<PinDescription>();
            this.outputs = new List<PinDescription> { new InstancePinDescription() };
            initializer(this);
        }

        public IVLNodeDescriptionFactory Factory => factory;

        public string Name => typeof(T).Name;

        public string Category => typeof(T).Namespace;

        public bool Fragmented => true;

        public IReadOnlyList<IVLPinDescription> Inputs => inputs;

        public IReadOnlyList<IVLPinDescription> Outputs => outputs;

        public IEnumerable<Message> Messages => Array.Empty<Message>();

        public IObservable<object> Invalidated => Observable.Empty<object>();

        public string Summary => typeof(T).GetSummary();

        public string Remarks => typeof(T).GetRemarks();

        public IVLNode CreateInstance(NodeContext context)
        {
            Initialization.InitAvalonia();
            return new Node(this, context, createInstance());
        }

        public bool OpenEditor() => false;

        sealed class Node : FactoryBasedVLNode, IVLNode
        {
            private readonly NodeDescription<T> nodeDescription;

            public Node(NodeDescription<T> nodeDescription, NodeContext nodeContext, T instance)
                : base(nodeContext)
            {
                this.nodeDescription = nodeDescription;
                Inputs = nodeDescription.inputs.Select(p => p.CreatePin(instance)).ToArray();
                Outputs = nodeDescription.outputs.Select(p => p.CreatePin(instance)).ToArray();
            }

            public IVLNodeDescription NodeDescription => nodeDescription;

            public IVLPin[] Inputs { get; }

            public IVLPin[] Outputs { get; }

            public void Dispose()
            {
                if (Outputs[0] is IDisposable disposable)
                    disposable.Dispose();
            }

            public void Update()
            {
            }
        }

        public void AddInput<TValue>(string name, Func<T, AvaloniaList<TValue>> getter)
        {
            inputs.Add(new ListPinDescription<TValue>(name, getter));
        }

        public void AddObservableInput<TValue>(AvaloniaProperty<TValue> property)
        {
            inputs.Add(new ObservablePropertyPinDescription<TValue>(property));
        }

        public void AddObservableInAndOut<TValue>(AvaloniaProperty<TValue> property)
        {
            inputs.Add(new ObservablePropertyPinDescription<TValue>(property));
            outputs.Add(new ObservablePropertyPinDescription<TValue>(property));
        }

        public void AddObservableOutput<TEventArgs>(RoutedEvent<TEventArgs> @event) where TEventArgs : RoutedEventArgs
        {
            outputs.Add(new EventPinDescription<TEventArgs>(@event));
        }

        public abstract class PinDescription : IVLPinDescription
        {
            public abstract string Name { get; }
            public abstract Type Type { get; }
            public abstract object DefaultValue { get; }

            public abstract IVLPin CreatePin(T instance);
        }

        sealed class InstancePinDescription : PinDescription
        {
            public override string Name => "Output";
            public override Type Type => typeof(T);
            public override object DefaultValue => default;

            public override IVLPin CreatePin(T instance) => new Pin() { Value = instance };

            class Pin : IVLPin<T>
            {
                public T Value { get; set; }
                object IVLPin.Value { get => Value; set => throw new NotImplementedException(); }
            }
        }

        sealed class ObservablePropertyPinDescription<TValue> : PinDescription
        {
            private readonly AvaloniaProperty<TValue> property;

            public ObservablePropertyPinDescription(AvaloniaProperty<TValue> property)
            {
                this.property = property;
            }

            public override string Name => property.Name;
            public override Type Type => typeof(IObservable<TValue>);
            public override object DefaultValue => property is StyledProperty<TValue> s ? s.GetDefaultValue(typeof(T)) : default(TValue);

            public override IVLPin CreatePin(T instance) => new Pin(instance, property);

            class Pin : IVLPin<IObservable<TValue>>
            {
                readonly T instance;
                readonly AvaloniaProperty<TValue> property;

                IObservable<TValue> value;
                IDisposable subscription;

                public Pin(T instance, AvaloniaProperty<TValue> property)
                {
                    this.instance = instance;
                    this.property = property;
                    this.value = instance.GetObservable(property);
                }

                public IObservable<TValue> Value
                { 
                    get => value;
                    set
                    {
                        if (value != this.value)
                        {
                            this.value = value;
                            subscription?.Dispose();
                            if (value != null && value != VL.Lib.Reactive.ObservableNodes.Never<TValue>())
                                subscription = instance.Bind(property, value, priority: Avalonia.Data.BindingPriority.Style);
                            else
                            {
                                instance.ClearValue(property);
                                subscription = null;
                            }
                        }
                    }
                }

                object IVLPin.Value 
                { 
                    get => Value; 
                    set => Value = (IObservable<TValue>)value; 
                }
            }
        }

        sealed class EventPinDescription<TEventArgs> : PinDescription
             where TEventArgs : RoutedEventArgs
        {
            private readonly RoutedEvent<TEventArgs> @event;

            public EventPinDescription(RoutedEvent<TEventArgs> @event)
            {
                this.@event = @event;
            }

            public override string Name => @event.Name;
            public override Type Type => typeof(IObservable<TEventArgs>);
            public override object DefaultValue => default;

            public override IVLPin CreatePin(T instance) => new Pin(instance, @event);

            class Pin : IVLPin<IObservable<TEventArgs>>
            {
                readonly IObservable<TEventArgs> observable;

                public Pin(T instance, RoutedEvent<TEventArgs> @event)
                {
                    if (instance is IInteractive interactive)
                        this.observable = interactive.GetObservable(@event);
                    else
                        this.observable = Observable.Empty<TEventArgs>();
                }

                public IObservable<TEventArgs> Value
                {
                    get => observable;
                    set => throw new NotSupportedException();
                }

                object IVLPin.Value
                {
                    get => Value;
                    set => throw new NotSupportedException();
                }
            }
        }

        sealed class ListPinDescription<TValue> : PinDescription
        {
            private readonly Func<T, AvaloniaList<TValue>> getter;

            public ListPinDescription(string name, Func<T, AvaloniaList<TValue>> getter)
            {
                Name = name;
                this.getter = getter;
            }

            public override string Name { get; }
            public override Type Type => typeof(IEnumerable<TValue>);
            public override object DefaultValue => Array.Empty<TValue>();

            public override IVLPin CreatePin(T instance) => new Pin(getter(instance));

            class Pin : IVLPin<IEnumerable<TValue>>
            {
                readonly AvaloniaList<TValue> list;

                IEnumerable<TValue> value;

                public Pin(AvaloniaList<TValue> list)
                {
                    this.list = list;
                }

                public IEnumerable<TValue> Value
                {
                    get => list;
                    set
                    {
                        if (value != this.value)
                        {
                            this.value = value;
                            list.Clear();
                            if (value != null)
                                list.AddRange(value);
                        }
                    }
                }

                object IVLPin.Value
                {
                    get => Value;
                    set => Value = (IEnumerable<TValue>)value;
                }
            }
        }
    }
}
