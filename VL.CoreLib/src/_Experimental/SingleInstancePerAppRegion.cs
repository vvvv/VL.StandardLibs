#nullable enable
using System;
using System.Threading;
using VL.Core;
using VL.Core.Import;
using VL.Core.PublicAPI;
using VL.Lib.Animation;
using VL.Lib.Basics.Resources;
using VL.Lib.Experimental;

[assembly: ImportType(
    typeof(SingleInstancePerAppRegion<>),
    Name = "SingleInstance (PerApp Region)",
    Category = "Primitive.Experimental"
)]

namespace VL.Lib.Experimental
{
    /// <summary>
    /// The patch inlay of the <see cref="SingleInstancePerAppRegion{T}"/>.
    /// </summary>
    /// <remarks>
    /// Declared outside of the region class on purpose: nesting it causes issues with the
    /// IRegion&lt;SingleInstancePerAppRegion&lt;T&gt;.ISingleInstanceInlay&lt;T&gt;&gt; signature.
    /// </remarks>
    public interface ISingleInstanceInlay<T>
    {
        /// <summary>
        /// Executes the patch and returns its result.
        /// </summary>
        T Produce();
    }

    /// <summary>
    /// A region whose patch exists only once per app. All instances of the same region node
    /// (e.g. inside multiple instances of the surrounding patch) share one patch inlay.
    /// Only the first <c>Update</c> call within a frame executes the patch; all other instances
    /// get the cached value of that frame.
    /// </summary>
    [ProcessNode(FragmentSelection = FragmentSelection.Explicit)]
    [Region(SupportedBorderControlPoints = ControlPointType.None)]
    public sealed class SingleInstancePerAppRegion<T>
        : IRegion<ISingleInstanceInlay<T>>,
            IDisposable
    {
        private readonly UniqueId _nodeId;
        private readonly Func<UniqueId, IResourceHandle<SharedInstance>> _acquire;

        private Func<ISingleInstanceInlay<T>>? _factory;
        private IResourceHandle<SharedInstance>? _handle;
        private bool _disposed;

        [Fragment]
        public SingleInstancePerAppRegion(NodeContext nodeContext)
            : this(
                // Identifies the region node itself and is the same for all instances of the
                // surrounding patch. The full path would be unique per instance.
                nodeContext.Path.Stack.Peek(),
                nodeId => AcquirePerApp(nodeContext, nodeId)
            ) { }

        /// <summary>
        /// Test seam: allows to supply the node id and the pool the shared instance is taken from.
        /// </summary>
        internal SingleInstancePerAppRegion(
            UniqueId nodeId,
            Func<UniqueId, IResourceHandle<SharedInstance>> acquire
        )
        {
            _nodeId = nodeId;
            _acquire = acquire ?? throw new ArgumentNullException(nameof(acquire));
        }

        private static IResourceHandle<SharedInstance> AcquirePerApp(
            NodeContext nodeContext,
            UniqueId nodeId
        )
        {
            // Pool and frame clock are resolved via AppHost.Current; ensure it is the app this node lives in.
            using var scope = nodeContext.AppHost.MakeCurrent();

            var frameClock = Clocks.FrameClock;

            return ResourceProvider
                .NewPooledPerApp(nodeId, _ => new SharedInstance(frameClock))
                .GetHandle();
        }

        void IRegion<ISingleInstanceInlay<T>>.SetPatchInlayFactory(
            Func<ISingleInstanceInlay<T>> patchInlayFactory
        )
        {
            ArgumentNullException.ThrowIfNull(patchInlayFactory);

            // Called again with a new factory after each recompile. The existing inlay is kept on
            // purpose: VL forwards calls on an outdated inlay to its hot-swapped version, state included.
            _factory = patchInlayFactory;
        }

        /// <summary>
        /// Executes the shared patch once per frame across all instances of this region node.
        /// </summary>
        /// <param name="value">The value produced by the patch in the current frame.</param>
        [Fragment]
        public void Update(out T value)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var factory =
                _factory
                ?? throw new InvalidOperationException(
                    "Patch inlay factory has not been assigned."
                );

            _handle ??= _acquire(_nodeId);

            value = _handle.Resource.UpdateOncePerFrame(factory);
        }

        // Border control points are not supported (see Region attribute), so there is no input
        // or output data to pass. The value is returned via Produce's typed return value.
        void IRegion<ISingleInstanceInlay<T>>.AcknowledgeInput(
            in InputDescription description,
            object? outerValue
        ) { }

        void IRegion<ISingleInstanceInlay<T>>.RetrieveInput(
            in InputDescription description,
            ISingleInstanceInlay<T> patchInlay,
            out object? innerValue
        )
        {
            innerValue = null;
        }

        void IRegion<ISingleInstanceInlay<T>>.AcknowledgeOutput(
            in OutputDescription description,
            ISingleInstanceInlay<T> patchInlay,
            object? innerValue
        ) { }

        void IRegion<ISingleInstanceInlay<T>>.RetrieveOutput(
            in OutputDescription description,
            out object? outerValue
        )
        {
            outerValue = null;
        }

        [Fragment]
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            var handle = _handle;
            _handle = null;
            handle?.Dispose();

            _factory = null;
        }

        /// <summary>
        /// The patch inlay shared by all region instances of one app with the same node id.
        /// </summary>
        internal sealed class SharedInstance : IDisposable
        {
            private readonly object _lock = new();
            private readonly IDisposable _ticks;
            private ISingleInstanceInlay<T>? _inlay;
            private long _frame;
            private long _lastUpdatedFrame = -1;
            private T _value = default!;
            private bool _disposed;

            public SharedInstance(IFrameClock frameClock)
            {
                _ticks = frameClock.GetTicks().Subscribe(_ => Interlocked.Increment(ref _frame));
            }

            /// <summary>
            /// Executes the patch inlay on the first call within a frame. Subsequent calls within
            /// the same frame return the value of that first call.
            /// </summary>
            public T UpdateOncePerFrame(Func<ISingleInstanceInlay<T>> factory)
            {
                lock (_lock)
                {
                    ObjectDisposedException.ThrowIf(_disposed, this);

                    var frame = Interlocked.Read(ref _frame);
                    if (frame == _lastUpdatedFrame)
                        return _value;

                    _inlay ??= factory();
                    _value = _inlay.Produce();
                    _lastUpdatedFrame = frame;

                    return _value;
                }
            }

            public void Dispose()
            {
                _ticks.Dispose();
                lock (_lock)
                {
                    if (_disposed)
                        return;

                    _disposed = true;
                    (_inlay as IDisposable)?.Dispose();
                    _inlay = null;
                    _value = default!;
                }
            }
        }
    }
}
