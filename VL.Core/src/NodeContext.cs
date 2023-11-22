#nullable enable
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using VL.Core.Utils;
using VL.Lib.Animation;

namespace VL.Core
{
    /// <summary>
    /// Contains information about the environment in which a node was created.
    /// </summary>
    public sealed class NodeContext
    {
        // Shouldn't be used - for unit tests only
        [Obsolete("Node context needs to be bound to an app host", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly NodeContext Default = new NodeContext(appHost: AppHost.Global, parent: null, localId: default);

        /// <summary>
        /// The current root context provided by the current app host. See <see cref="AppHost.CurrentOrGlobal"/> and <see cref="AppHost.RootContext"/>.
        /// </summary>
        public static NodeContext CurrentRoot => AppHost.CurrentOrGlobal.RootContext;

        [Obsolete("Use the overload which takes the app host")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static NodeContext Create(UniqueId rootId) => Create(AppHost.CurrentOrGlobal, rootId);

        /// <summary>
        /// Creates a new root context.
        /// </summary>
        public static NodeContext Create(AppHost appHost) => Create(appHost, default);

        /// <summary>
        /// Creates a new root context.
        /// </summary>
        public static NodeContext Create(AppHost appHost, UniqueId rootId) => new NodeContext(appHost, null, rootId, definitionId: rootId);

        private readonly AppHost _appHost;
        private readonly NodeContext? _parent;
        private readonly UniqueId _localId;
        private readonly UniqueId? _definitionId;
        private ImmutableStack<UniqueId>? _stack;

        private NodeContext(AppHost appHost, NodeContext? parent, UniqueId localId, bool isImmutable = false, UniqueId? definitionId = null)
        {
            _appHost = appHost ?? throw new ArgumentNullException(nameof(appHost));
            _parent = parent;
            _localId = localId;
            IsImmutable = isImmutable;
            _definitionId = definitionId;

            Path = new NodePath(this);
        }

        public AppHost AppHost => _appHost;

        internal NodeContext? Parent => _parent;

        internal UniqueId LocalId => _localId;

        internal UniqueId? DefinitionId => _definitionId;

        /// <summary>
        /// The path to the node.
        /// </summary>
        public readonly NodePath Path;

        /// <summary>
        /// The VL factory.
        /// </summary>
        [Obsolete("Please use IVLFactory.Current instead")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public readonly IVLFactory Factory = DeferredVLFactory.Default;

        /// <summary>
        /// Whether or not the context is immutable. In an immutable context the state must not be modified.
        /// </summary>
        public readonly bool IsImmutable;

        /// <summary>
        /// Creates a new sub context.
        /// </summary>
        public NodeContext CreateSubContext(string documentId, string elementId) => CreateSubContext(new UniqueId(documentId, elementId));

        /// <summary>
        /// Creates a new sub context.
        /// </summary>
        public NodeContext CreateSubContext(UniqueId id) => new NodeContext(_appHost, this, id, IsImmutable, _definitionId);

        public NodeContext WithIsImmutable(bool value) => value != IsImmutable ? new NodeContext(_appHost, _parent, _localId, value, _definitionId) : this;

        public NodeContext WithDefinitionId(string documentId, string elementId) => WithDefinitionId(new UniqueId(documentId, elementId));

        public NodeContext WithDefinitionId(UniqueId value) => value != DefinitionId ? new NodeContext(_appHost, _parent, _localId, IsImmutable, value) : this;

        [Obsolete]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public UniqueId AppContext => Path.Stack.LastOrDefault();

        /// <summary>
        /// The frame clock.
        /// </summary>
        [Obsolete("Use Clocks.FrameClock")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public IFrameClock FrameClock => ServiceRegistry.Current.GetRequiredService<IFrameClock>();

        /// <summary>
        /// The real time clock.
        /// </summary>
        [Obsolete("Use Clocks.RealTimeClock")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public IClock RealTimeClock => ServiceRegistry.Current.GetRequiredService<IClock>();

        public ILogger GetLogger() => _appHost.LoggerFactory.CreateLogger(null, this);

        internal ImmutableStack<UniqueId> Stack
        {
            get
            {
                return _stack ??= Compute();

                ImmutableStack<UniqueId> Compute()
                {
                    using var builder = Pooled.GetList<UniqueId>();
                    Collect(builder.Value, this);
                    return ImmutableStack.CreateRange(builder.Value);

                    static void Collect(List<UniqueId> ids, NodeContext nodeContext)
                    {
                        if (nodeContext._parent != null)
                            Collect(ids, nodeContext._parent);

                        ids.Add(nodeContext.LocalId);
                    }
                }
            }
        }
    }
}
