using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using VL.Lib.Animation;

namespace VL.Core
{
    /// <summary>
    /// Contains information about the environment in which a node was created.
    /// </summary>
    public sealed class NodeContext
    {
        public static readonly NodeContext Default = new NodeContext(ImmutableStack<UniqueId>.Empty);

        /// <summary>
        /// Creates a new root context.
        /// </summary>
        /// <returns>The new root context.</returns>
        public static NodeContext Create(UniqueId rootId) => new NodeContext(ImmutableStack.Create(rootId));

        /// <summary>
        /// Creates a new context.
        /// </summary>
        /// <param name="path">The path to the node for which this context gets created.</param>
        /// <param name="isImmutable">Whether the context must be immutable.</param>
        public NodeContext(ImmutableStack<UniqueId> path, bool isImmutable = false)
        {
            Path = new NodePath(path);
            IsImmutable = isImmutable;
        }

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
        public NodeContext CreateSubContext(string documentId, string elementId, uint volatileId) => CreateSubContext(new UniqueId(documentId, elementId, volatileId));

        /// <summary>
        /// Creates a new sub context.
        /// </summary>
        public NodeContext CreateSubContext(UniqueId id) => new NodeContext(Path.Stack.Push(id), IsImmutable);

        public NodeContext WithIsImmutable(bool value) => value != IsImmutable ? new NodeContext(Path.Stack, value) : this;

        [Obsolete]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public UniqueId AppContext => Path.Stack.LastOrDefault();

        /// <summary>
        /// The frame clock.
        /// </summary>
        [Obsolete("Use Clocks.FrameClock")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public IFrameClock FrameClock => ServiceRegistry.Current.GetService<IFrameClock>();

        /// <summary>
        /// The real time clock.
        /// </summary>
        [Obsolete("Use Clocks.RealTimeClock")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public IClock RealTimeClock => ServiceRegistry.Current.GetService<IClock>();
    }
}
