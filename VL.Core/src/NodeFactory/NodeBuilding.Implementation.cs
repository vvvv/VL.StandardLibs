#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading;
using VL.Core.Diagnostics;

namespace VL.Core
{
    partial class NodeBuilding
    {
        sealed class DelegateNodeDescriptionFactory : IVLNodeDescriptionFactory
        {
            readonly NodeFactoryCache FCache;
            readonly Lazy<FactoryImpl> FImpl;

            public DelegateNodeDescriptionFactory(
                NodeFactoryCache cache,
                string identifier,
                Func<IVLNodeDescriptionFactory, FactoryImpl> nodesFactory,
                string? filePath = null)
            {
                FCache = cache;
                Identifier = identifier;
                FilePath = filePath ?? nodesFactory.Method.DeclaringType?.Assembly.Location;
                FImpl = new Lazy<FactoryImpl>(() => nodesFactory(this), LazyThreadSafetyMode.ExecutionAndPublication);
            }

            public string Identifier { get; }

            public string? FilePath { get; }

            public ImmutableArray<IVLNodeDescription> NodeDescriptions => FImpl.Value.Nodes;

            public IObservable<object> Invalidated => FImpl.Value.Invalidated;

            public IVLNodeDescriptionFactory? ForPath(string path)
            {
                var factory = FImpl.Value.ForPath?.Invoke(path);
                if (factory != null)
                {
                    var identifier = $"{Identifier} ({path})";
                    return FCache.GetOrAdd(identifier, () =>
                    {
                        return new DelegateNodeDescriptionFactory(FCache, identifier, factory, filePath: path);
                    });
                }
                return null;
            }

            public void Export(ExportContext exportContext) => FImpl.Value.Export?.Invoke(exportContext);
        }

        public class FactoryImpl
        {
            public readonly ImmutableArray<IVLNodeDescription> Nodes;
            public readonly IObservable<object> Invalidated;
            public readonly Func<string, Func<IVLNodeDescriptionFactory, FactoryImpl>>? ForPath;
            public readonly Action<ExportContext>? Export;

            public FactoryImpl(ImmutableArray<IVLNodeDescription> nodes = default, IObservable<object>? invalidated = default, Func<string, Func<IVLNodeDescriptionFactory, FactoryImpl>>? forPath = default, Action<ExportContext>? export = default)
            {
                Nodes = !nodes.IsDefault ? nodes : ImmutableArray<IVLNodeDescription>.Empty;
                Invalidated = invalidated ?? Observable.Empty<IVLNodeDescriptionFactory>();
                ForPath = forPath;
                Export = export;
            }
        }

        sealed class NodeDescription : IVLNodeDescription, ITaggedInfo
        {
            readonly Lazy<NodeImplementation> FImpl;

            public NodeDescription(
                IVLNodeDescriptionFactory factory,
                string name,
                string category,
                bool fragmented,
                IObservable<object> invalidated,
                Func<NodeDescriptionBuildContext, NodeImplementation> init,
                string? tags = default)
            {
                Name = name;
                Category = category;
                Fragmented = fragmented;
                Factory = factory;
                Invalidated = invalidated ?? Observable.Empty<object>();
                Tags = InfoUtils.ParseTags(tags);
                FImpl = new Lazy<NodeImplementation>(() => init(new NodeDescriptionBuildContext(this)), LazyThreadSafetyMode.ExecutionAndPublication);
            }

            public IVLNodeDescriptionFactory Factory { get; }

            public string Name { get; }

            public string Category { get; }

            public string? FilePath => FImpl.Value.FilePath;

            public bool Fragmented { get; }

            public IReadOnlyList<IVLPinDescription> Inputs => FImpl.Value.Inputs;

            public IReadOnlyList<IVLPinDescription> Outputs => FImpl.Value.Outputs;

            public IEnumerable<Message> Messages => FImpl.Value.Messages;

            public IObservable<object> Invalidated { get; }

            public string? Summary => FImpl.Value.Summary;

            public string? Remarks => FImpl.Value.Remarks;

            public ImmutableArray<string> Tags { get; }

            public IVLNode CreateInstance(NodeContext context)
            {
                var buildContext = new NodeInstanceBuildContext(this, context);
                return FImpl.Value.CreateInstance(buildContext);
            }

            public Func<bool>? OpenEditorAction => FImpl.Value.OpenEditorAction;

            public override string ToString()
            {
                return "NodeDesc: " + Name + " [" + Category + "]";
            }
        }

        sealed class PinDescription : IVLPinDescription, IInfo
        {
            public static PinDescription New<T>(string name, T? defaultValue = default)
            {
                return new PinDescription(name, typeof(T), defaultValue);
            }

            public PinDescription(string name, Type type, object? defaultValue)
            {
                Name = name;
                Type = type;
                DefaultValue = defaultValue;
            }

            public string Name { get; }

            public Type Type { get; }

            public object? DefaultValue { get; }

            public string? Summary { get; set; }

            public string? Remarks { get; set; }
        }

        sealed class NodeInstance : FactoryBasedVLNode, IVLNode
        {
            readonly Action? update, dispose;

            public NodeInstance(NodeContext nodeContext, IVLNodeDescription nodeDescription, IVLPin[] inputs, IVLPin[] outputs, Action? update = default, Action? dispose = default)
                : base(nodeContext)
            {
                this.NodeDescription = nodeDescription;
                this.Inputs = inputs;
                this.Outputs = outputs;
                this.update = update;
                this.dispose = dispose;
            }

            public IVLNodeDescription NodeDescription { get; }

            public IVLPin[] Inputs { get; }

            public IVLPin[] Outputs { get; }

            public void Update() => update?.Invoke();

            public void Dispose() => dispose?.Invoke();
        }

        sealed class PinInstance<T> : IVLPin<T>
        {
            readonly Func<T?>? getter;
            readonly Action<T?>? setter;
            T? value;

            public PinInstance(Func<T?>? getter = default, Action<T?>? setter = default, T? value = default)
            {
                this.getter = getter;
                this.setter = setter;
                this.value = value;
            }

            public T? Value
            {
                get => getter != null ? getter.Invoke() : value;
                set
                {
                    this.value = value;
                    setter?.Invoke(value);
                }
            }

            object? IVLPin.Value { get => Value; set => Value = (T?)value; }
        }

        public class NodeImplementation
        {
            public readonly ImmutableArray<IVLPinDescription> Inputs;
            public readonly ImmutableArray<IVLPinDescription> Outputs;
            public readonly Func<NodeInstanceBuildContext, IVLNode> CreateInstance;
            public readonly ImmutableArray<Message> Messages;
            public readonly Func<bool>? OpenEditorAction;
            public readonly string? Summary;
            public readonly string? Remarks;
            public readonly string? Tags;
            public readonly string? FilePath;

            internal NodeImplementation(
                ImmutableArray<IVLPinDescription> inputs, 
                ImmutableArray<IVLPinDescription> outputs, 
                Func<NodeInstanceBuildContext, IVLNode> createInstance, 
                ImmutableArray<Message> messages = default, 
                Func<bool>? openEditorAction = default,
                string? summary = default,
                string? remarks = default,
                string? tags = default,
                string? filePath = default)
            {
                Inputs = !inputs.IsDefault ? inputs : ImmutableArray<IVLPinDescription>.Empty;
                Outputs = !outputs.IsDefault ? outputs : ImmutableArray<IVLPinDescription>.Empty;
                CreateInstance = createInstance ?? throw new ArgumentNullException(nameof(createInstance));
                Messages = !messages.IsDefault ? messages : ImmutableArray<Message>.Empty;
                OpenEditorAction = openEditorAction;
                Summary = summary;
                Remarks = remarks;
                Tags = tags;
                FilePath = filePath;
            }
        }

        public sealed class NodeDescriptionBuildContext
        {
            public readonly IVLNodeDescription NodeDescription;

            internal NodeDescriptionBuildContext(IVLNodeDescription nodeDescription)
            {
                NodeDescription = nodeDescription;
            }

            public IVLPinDescription Pin(string name, Type type, object? defaultValue = null, string? summary = default, string? remarks = default)
            {
                return new PinDescription(name, type, defaultValue) { Summary = summary, Remarks = remarks };
            }

            public NodeImplementation Node(
                IEnumerable<IVLPinDescription> inputs,
                IEnumerable<IVLPinDescription> outputs,
                Func<NodeInstanceBuildContext, IVLNode> newNode,
                IEnumerable<Message>? messages = default,
                Func<bool>? openEditorAction = default,
                string? summary = default,
                string? remarks = default,
                string? filePath = default)
            {
                return new NodeImplementation(
                    inputs?.ToImmutableArray() ?? ImmutableArray<IVLPinDescription>.Empty,
                    outputs?.ToImmutableArray() ?? ImmutableArray<IVLPinDescription>.Empty,
                    newNode,
                    messages: messages?.ToImmutableArray() ?? default,
                    openEditorAction: openEditorAction,
                    summary: summary,
                    remarks: remarks,
                    filePath: filePath);
            }

            // Overload was part of 2021.3 release. Don't change or it breaks compatibility!
            [Obsolete("Please use Node instead")]
            public NodeImplementation Implementation(
                IEnumerable<IVLPinDescription> inputs,
                IEnumerable<IVLPinDescription> outputs,
                Func<NodeInstanceBuildContext, IVLNode> newNode,
                IEnumerable<Message>? messages = default,
                Func<bool>? openEditor = default,
                string? summary = default,
                string? remarks = default)
            {
                return NewNode(inputs, outputs, newNode, messages, openEditor, summary, remarks);
            }

            // Overload was part of 2021.4 release. Don't change or it breaks compatibility!
            [Obsolete("Please use Node instead")]
            public NodeImplementation NewNode(
                IEnumerable<IVLPinDescription> inputs,
                IEnumerable<IVLPinDescription> outputs,
                Func<NodeInstanceBuildContext, IVLNode> newNode,
                IEnumerable<Message>? messages = default,
                Func<bool>? openEditor = default,
                string? summary = default,
                string? remarks = default)
            {
                return new NodeImplementation(
                    inputs?.ToImmutableArray() ?? ImmutableArray<IVLPinDescription>.Empty,
                    outputs?.ToImmutableArray() ?? ImmutableArray<IVLPinDescription>.Empty,
                    newNode,
                    messages: messages?.ToImmutableArray() ?? default,
                    openEditorAction: openEditor,
                    summary: summary,
                    remarks: remarks);
            }

            // Overload was part of 2021.3 release. Don't change or it breaks compatibility!
            [Obsolete("Please use Node instead")]
            public NodeImplementation Implementation<T>(
                IEnumerable<IVLPinDescription> inputs,
                IEnumerable<IVLPinDescription> outputs,
                Func<NodeInstanceBuildContext, IVLNode> newNode,
                IObservable<T> invalidated,
                IEnumerable<Message>? messages = default,
                Func<bool>? openEditor = default,
                string? summary = default,
                string? remarks = default)
            {
                return NewNode(inputs, outputs, newNode, messages, openEditor, summary, remarks);
            }
        }

        public sealed class NodeInstanceBuildContext
        {
            public readonly IVLNodeDescription NodeDescription;
            public readonly NodeContext NodeContext;

            internal NodeInstanceBuildContext(IVLNodeDescription nodeDescription, NodeContext nodeContext)
            {
                NodeDescription = nodeDescription;
                NodeContext = nodeContext;
            }

            public IVLNode Node(IEnumerable<IVLPin> inputs, IEnumerable<IVLPin> outputs, Action? update = default, Action? dispose = default)
            {
                return new NodeInstance(
                    NodeContext, 
                    NodeDescription, 
                    inputs?.ToArray() ?? Array.Empty<IVLPin>(),
                    outputs?.ToArray() ?? Array.Empty<IVLPin>(),
                    update, 
                    dispose);
            }

            public IVLPin<T> Input<T>(Action<T?>? setter = default, T? value = default)
            {
                return new PinInstance<T>(default, setter, value);
            }

            public IVLPin<T> Input<T>(T value)
            {
                return new PinInstance<T>(default, default, value);
            }

            public IVLPin<T> Output<T>(Func<T?>? getter = default)
            {
                return new PinInstance<T>(getter);
            }
        }
    }
}
#nullable restore