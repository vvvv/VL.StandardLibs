using Stride.Core.Collections;
using Stride.Engine;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using VL.Core;
using VL.Core.Diagnostics;
using VL.Lib.Basics.Resources;
using VL.Stride.Engine;

namespace VL.Stride
{
    static class FactoryExtensions
    {
        public static CustomNodeDesc<T> NewNode<T>(this IVLNodeDescriptionFactory factory,
           Func<NodeContext, T> ctor,
           string name = default,
           string category = default,
           bool copyOnWrite = true,
           bool hasStateOutput = true)
            where T : class
        {
            return new CustomNodeDesc<T>(factory,
                ctor: ctx =>
                {
                    var instance = ctor(ctx);
                    return (instance, default);
                },
                name: name,
                category: category,
                copyOnWrite: copyOnWrite,
                hasStateOutput: hasStateOutput);
        }

        public static CustomNodeDesc<T> NewNode<T>(this IVLNodeDescriptionFactory factory, 
            string name = default, 
            string category = default, 
            bool copyOnWrite = true,
            Action<T> init = default,
            bool hasStateOutput = true) 
            where T : class, new()
        {
            return new CustomNodeDesc<T>(factory, 
                ctor: ctx =>
                {
                    var instance = new T();
                    init?.Invoke(instance);
                    return (instance, default);
                }, 
                name: name, 
                category: category,
                copyOnWrite: copyOnWrite,
                hasStateOutput: hasStateOutput);
        }

        public static CustomNodeDesc<TComponent> NewComponentNode<TComponent>(this IVLNodeDescriptionFactory factory, string category, Action<TComponent> init = null, string name = null)
            where TComponent : EntityComponent, new()
        {
            return new CustomNodeDesc<TComponent>(factory,
                name: name,
                ctor: nodeContext =>
                {
                    var container = new CompositeDisposable();
                    var component = new TComponent().WithParentManager(nodeContext, container);
                    init?.Invoke(component);
                    return (component, () => container.Dispose());
                }, 
                category: category, 
                copyOnWrite: false);
        }

        public static IVLNodeDescription WithEnabledPin<TComponent>(this CustomNodeDesc<TComponent> node)
            where TComponent : ActivableEntityComponent
        {
            return node.AddCachedInput("Enabled", x => x.Enabled, (x, v) => x.Enabled = v, true);
        }
    }

    class CustomNodeDesc<TInstance> : IVLNodeDescription, ITaggedInfo
        where TInstance : class
    {
        readonly List<CustomPinDesc> inputs = new List<CustomPinDesc>();
        readonly List<CustomPinDesc> outputs = new List<CustomPinDesc>();
        readonly Func<NodeContext, (TInstance, Action)> ctor;

        public CustomNodeDesc(IVLNodeDescriptionFactory factory, Func<NodeContext, (TInstance, Action)> ctor, 
            string name = default, 
            string category = default, 
            bool copyOnWrite = true, 
            bool hasStateOutput = true,
            ImmutableArray<string> tags = default)
        {
            Factory = factory;
            this.ctor = ctor;

            Name = name ?? typeof(TInstance).Name;
            Category = category ?? string.Empty;
            CopyOnWrite = copyOnWrite;
            Tags = tags.IsDefault ? ImmutableArray<string>.Empty : tags;

            if (hasStateOutput)
                AddOutput("Output", x => x);
        }

        public IVLNodeDescriptionFactory Factory { get; }

        public string Name { get; }

        public string Category { get; }

        public bool Fragmented => true;

        public bool CopyOnWrite { get; }

        public IReadOnlyList<IVLPinDescription> Inputs => inputs;

        public IReadOnlyList<IVLPinDescription> Outputs => outputs;

        public IEnumerable<Message> Messages => Enumerable.Empty<Message>();

        public string Summary => typeof(TInstance).GetSummary();

        public string Remarks => typeof(TInstance).GetRemarks();

        public ImmutableArray<string> Tags { get; }

        public IObservable<object> Invalidated => Observable.Empty<object>();

        public IVLNode CreateInstance(NodeContext context)
        {
            var (instance, onDispose) = ctor(context);

            var node = new Node(context)
            {
                NodeDescription = this
            };

            var inputs = this.inputs.Select(p => p.CreatePin(node, instance)).ToArray();
            var outputs = this.outputs.Select(p => p.CreatePin(node, instance)).ToArray();

            node.Inputs = inputs;
            node.Outputs = outputs;

            if (CopyOnWrite)
            {
                node.updateAction = () =>
                {
                    if (node.needsUpdate)
                    {
                        node.needsUpdate = false;
                        // TODO: Causes render pipeline to crash
                        //if (instance is IDisposable disposable)
                        //    disposable.Dispose();

                        instance = ctor(context).Item1;

                        // Copy the values
                        foreach (var input in inputs)
                            input.Update(instance);

                        foreach (var output in outputs)
                            output.Instance = instance;
                    }
                };
                node.disposeAction = () =>
                {
                    // TODO: Causes render pipeline to crash
                    //if (instance is IDisposable disposable)
                    //    disposable.Dispose();
                };
            }
            else
            {
                node.updateAction = () =>
                {
                    if (node.needsUpdate)
                    {
                        node.needsUpdate = false;
                    }
                };
                node.disposeAction = () =>
                {
                    if (instance is IDisposable disposable)
                        disposable.Dispose();
                    onDispose?.Invoke();
                };
            }
            return node;
        }

        public CustomNodeDesc<TInstance> AddInput<T>(string name, Func<TInstance, T> getter, Action<TInstance, T> setter, string summary = default, string remarks = default, bool isVisible = true)
        {
            inputs.Add(new CustomPinDesc(name, summary, remarks)
            {
                Name = name.InsertSpaces(),
                Type = typeof(T),
                CreatePin = (node, instance) => new InputPin<T>(node, instance, getter, setter, getter(instance)),
                IsVisible = isVisible
            });
            return this;
        }

        public CustomNodeDesc<TInstance> AddInput<T>(string name, Func<TInstance, T> getter, Action<TInstance, T> setter, T defaultValue, string summary = default, string remarks = default, bool isVisible = true)
        {
            inputs.Add(new CustomPinDesc(name, summary, remarks)
            {
                Name = name.InsertSpaces(),
                Type = typeof(T),
                DefaultValue = defaultValue,
                CreatePin = (node, instance) => new InputPin<T>(node, instance, getter, setter, defaultValue),
                IsVisible = isVisible
            });
            return this;
        }

        public CustomNodeDesc<TInstance> AddCachedInput<T>(string name, Func<TInstance, T> getter, Action<TInstance, T> setter, Func<T, T, bool> equals = default, string summary = default, string remarks = default, bool isVisible = true)
        {
            inputs.Add(new CustomPinDesc(name, summary, remarks)
            {
                Name = name.InsertSpaces(),
                Type = typeof(T),
                CreatePin = (node, instance) => new CachedInputPin<T>(node, instance, getter, setter, getter(instance), equals),
                IsVisible = isVisible
            });
            return this;
        }

        public CustomNodeDesc<TInstance> AddCachedInput<T>(string name, Func<TInstance, T> getter, Action<TInstance, T> setter, T defaultValue, string summary = default, string remarks = default, bool isVisible = true)
        {
            inputs.Add(new CustomPinDesc(name, summary, remarks)
            {
                Name = name.InsertSpaces(),
                Type = typeof(T),
                DefaultValue = defaultValue,
                CreatePin = (node, instance) => new CachedInputPin<T>(node, instance, getter, setter, defaultValue),
                IsVisible = isVisible
            });
            return this;
        }

        public CustomNodeDesc<TInstance> AddOptimizedInput<T>(string name, Func<TInstance, T> getter, Action<TInstance, T> setter, Func<T, T, bool> equals = default, string summary = default, string remarks = default, bool isVisible = true)
        {
            inputs.Add(new CustomPinDesc(name, summary, remarks)
            {
                Name = name.InsertSpaces(),
                Type = typeof(T),
                CreatePin = (node, instance) => new OptimizedInputPin<T>(node, instance, getter, setter, getter(instance)),
                IsVisible = isVisible
            });
            return this;
        }

        public CustomNodeDesc<TInstance> AddOptimizedInput<T>(string name, Func<TInstance, T> getter, Action<TInstance, T> setter, T defaultValue, string summary = default, string remarks = default, bool isVisible = true)
        {
            inputs.Add(new CustomPinDesc(name, summary, remarks)
            {
                Name = name.InsertSpaces(),
                Type = typeof(T),
                DefaultValue = defaultValue,
                CreatePin = (node, instance) => new OptimizedInputPin<T>(node, instance, getter, setter, defaultValue),
                IsVisible = isVisible
            });
            return this;
        }

        static bool SequenceEqual<T>(IEnumerable<T> a, IEnumerable<T> b)
        {
            if (a is null)
                return b is null;
            if (b is null)
                return false;
            return a.SequenceEqual(b);
        }

        public CustomNodeDesc<TInstance> AddCachedListInput<T>(string name, Func<TInstance, IList<T>> getter, Action<TInstance> updateInstanceAfterSetter = null)
        {
            return AddCachedInput<IReadOnlyList<T>>(name, 
                getter: instance => (IReadOnlyList<T>)getter(instance),
                equals: SequenceEqual,
                setter: (x, v) =>
                {
                    var currentItems = getter(x);
                    currentItems.Clear();

                    if (v != null)
                    {
                        foreach (var item in v)
                        {
                            if (item != null)
                            {
                                currentItems.Add(item);
                            }
                        }
                    }

                    updateInstanceAfterSetter?.Invoke(x);
                });
        }

        public CustomNodeDesc<TInstance> AddCachedListInput<T>(string name, Func<TInstance, T[]> getter, Action<TInstance, T[]> setter)
        {
            return AddCachedInput<IReadOnlyList<T>>(name,
                getter: getter,
                equals: SequenceEqual,
                setter: (x, v) =>
                {
                    var newItems = v?.Where(i => i != null);
                    setter(x, newItems?.ToArray());
                });
        }

        public CustomNodeDesc<TInstance> AddCachedListInput<T>(string name, Func<TInstance, IndexingDictionary<T>> getter)
            where T : class
        {
            return AddCachedInput<IReadOnlyList<T>>(name,
                getter: x => getter(x).Values.ToList(),
                equals: SequenceEqual,
                setter: (x, v) =>
                {
                    var currentItems = getter(x);
                    currentItems.Clear();

                    if (v != null)
                    {
                        for (int i = 0; i < v.Count; i++)
                        {
                            currentItems[i] = v[i];
                        }
                    }
                });
        }

        public CustomNodeDesc<TInstance> AddOutput<T>(string name, Func<TInstance, T> getter, string summary = default, string remarks = default, bool isVisible = true)
        {
            outputs.Add(new CustomPinDesc(name, summary, remarks)
            {
                Name = name.InsertSpaces(),
                Type = typeof(T),
                CreatePin = (node, instance) => new OutputPin<T>(node, instance, getter),
                IsVisible = isVisible
            });
            return this;
        }

        public CustomNodeDesc<TInstance> AddCachedOutput<T>(string name, Func<TInstance, T> getter, string summary = default, string remarks = default, bool isVisible = true)
        {
            outputs.Add(new CustomPinDesc(name, summary, remarks)
            {
                Name = name.InsertSpaces(),
                Type = typeof(T),
                CreatePin = (node, instance) => new CachedOutputPin<T>(node, instance, getter),
                IsVisible = isVisible
            });
            return this;
        }

        public CustomNodeDesc<TInstance> AddCachedOutput<T>(string name, Func<CompositeDisposable, Func<TInstance, T>> ctor, string summary = default, string remarks = default, bool isVisible = true)
        {
            outputs.Add(new CustomPinDesc(name, summary, remarks)
            {
                Name = name.InsertSpaces(),
                Type = typeof(T),
                CreatePin = (node, instance) =>
                {
                    var disposable = new CompositeDisposable();
                    var getter = ctor(disposable);
                    return new CachedOutputPin<T>(node, instance, getter, disposable);
                },
                IsVisible = isVisible
            });
            return this;
        }

        class CustomPinDesc : IVLPinDescription, IInfo, IVLPinDescriptionWithVisibility
        {
            readonly string memberName;
            string summary;
            string remarks;

            public CustomPinDesc(string memberName, string summary = default, string remarks = default)
            {
                this.memberName = memberName;
                this.summary = summary;
                this.remarks = remarks;
            }

            public string Name { get; set; }

            public Type Type { get; set; }

            public object DefaultValue { get; set; }

            public Func<Node, TInstance, Pin> CreatePin { get; set; }

            public string Summary => summary ?? (summary = typeof(TInstance).GetSummary(memberName));

            public string Remarks => remarks ?? (remarks = typeof(TInstance).GetRemarks(memberName));

            public bool IsVisible { get; set; } = true;
        }

        abstract class Pin : IVLPin
        {
            public readonly Node Node;
            public TInstance Instance;

            public Pin(Node node, TInstance instance)
            {
                Node = node;
                Instance = instance;
            }

            public abstract object BoxedValue { get; set; }

            // Update the pin by copying the underlying value to the new instance
            public virtual void Update(TInstance instance)
            {
                Instance = instance;
            }

            object IVLPin.Value
            {
                get => BoxedValue;
                set => BoxedValue = value;
            }
        }

        abstract class Pin<T> : Pin, IVLPin<T>
        {
            public Func<TInstance, T> getter;
            public Action<TInstance, T> setter;

            public Pin(Node node, TInstance instance) : base(node, instance)
            {
            }

            public override sealed object BoxedValue 
            { 
                get => Value; 
                set => Value = (T)value;
            }

            public abstract T Value { get; set; }
        }

        class InputPin<T> : Pin<T>, IVLPin
        {
            public InputPin(Node node, TInstance instance, Func<TInstance, T> getter, Action<TInstance, T> setter, T initialValue)
                : base(node, instance)
            {
                this.getter = getter;
                this.setter = setter;
                this.InitialValue = initialValue;
                setter(instance, initialValue);
            }

            public T InitialValue { get; }

            public override T Value
            {
                get => getter(Instance);
                set
                {
                    // Normalize the value first
                    if (value is null)
                        value = InitialValue;

                    setter(Instance, value);
                    Node.needsUpdate = true;
                }
            }

            public override void Update(TInstance instance)
            {
                var currentValue = getter(Instance);
                base.Update(instance);
                setter(instance, currentValue);
            }
        }

        class CachedInputPin<T> : InputPin<T>, IVLPin
        {
            readonly Func<T, T, bool> equals;
            T lastValue;

            public CachedInputPin(Node node, TInstance instance, Func<TInstance, T> getter, Action<TInstance, T> setter, T initialValue, Func<T, T, bool> equals = default) 
                : base(node, instance, getter, setter, initialValue)
            {
                this.equals = equals ?? EqualityComparer<T>.Default.Equals;
                lastValue = initialValue;
            }

            public override T Value
            {
                get => getter(Instance);
                set
                {
                    if (!equals(value, lastValue))
                    {
                        lastValue = value;
                        base.Value = value;
                    }
                }
            }
        }

        class OptimizedInputPin<T> : InputPin<T>, IVLPin
        {
            readonly Func<T, T, bool> equals;

            public OptimizedInputPin(Node node, TInstance instance, Func<TInstance, T> getter, Action<TInstance, T> setter, T initialValue)
                : base(node, instance, getter, setter, initialValue)
            {
                this.equals = equals ?? EqualityComparer<T>.Default.Equals;
            }

            public override T Value
            {
                get => getter(Instance);
                set
                {
                    if (!equals(value, Value))
                        base.Value = value;
                }
            }
        }

        class OutputPin<T> : Pin<T>
        {
            public OutputPin(Node node, TInstance instance, Func<TInstance, T> getter) 
                : base(node, instance)
            {
                this.getter = getter;
            }

            public override T Value 
            {
                get
                {
                    if (Node.needsUpdate)
                        Node.Update();
                    return getter(Instance);
                }
                set => throw new InvalidOperationException(); 
            }
        }

        class CachedOutputPin<T> : OutputPin<T>, IDisposable
        {
            readonly IDisposable disposeable;

            public CachedOutputPin(Node node, TInstance instance, Func<TInstance, T> getter, IDisposable disposeable = default)
                : base(node, instance, getter)
            {
                this.disposeable = disposeable;
            }

            public override T Value
            {
                get
                {
                    if (Node.needsUpdate)
                    {
                        Node.Update();
                        cachedValue = getter(Instance);
                    }
                    return cachedValue;
                }
                set => throw new InvalidOperationException();
            }
            T cachedValue;

            public void Dispose()
            {
                disposeable?.Dispose();
            }
        }

        class Node : FactoryBasedVLNode, IVLNode
        {
            public Action updateAction;
            public Action disposeAction;
            public bool needsUpdate = true;

            public Node(NodeContext nodeContext)
                : base(nodeContext)
            {
            }

            public IVLNodeDescription NodeDescription { get; set; }

            public IVLPin[] Inputs { get; set; }

            public IVLPin[] Outputs { get; set; }

            public void Dispose()
            {
                foreach (var p in Outputs)
                    if (p is IDisposable d)
                        d.Dispose();

                disposeAction?.Invoke();
            }

            public void Update() => updateAction?.Invoke();
        }
    }
}
