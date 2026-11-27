#nullable enable
using System;
using System.Collections.Generic;
using VL.Core;
using VL.Core.Import;
using VL.Core.PublicAPI;
using VL.Lang;
using VL.Lib.Control;

[assembly: ImportType(
    typeof(IfAssignedRegion<>),
    Name = "IfAssigned",
    Category = "Control.Advanced"
)]

namespace VL.Lib.Control
{
    /// <summary>
    /// The patch inlay of the <see cref="IfAssignedRegion{T}"/>.
    /// </summary>
    /// <remarks>
    /// Declared outside of the region class on purpose: nesting it causes issues with the
    /// IRegion&lt;IfAssignedRegion&lt;T&gt;.IIfAssignedInlay&lt;T&gt;&gt; signature.
    /// </remarks>
    public interface IIfAssignedInlay<T>
    {
        /// <summary>
        /// Executes the patch. The parameter shows up as a pin inside of the region.
        /// </summary>
        /// <param name="input">The assigned input value, guaranteed not to be null.</param>
        void Update(T input);
    }

    /// <summary>
    /// Executes the patch inside only if the input is assigned (not null) and passes the value into it.
    /// Behaves like an If region whose condition is an IsAssigned node on the input.
    /// Accumulators pass their values through. In frames the patch doesn't run, each accumulator output
    /// returns the value of its input.
    /// </summary>
    /// <remarks>
    /// Values of a non-nullable value type are never null: the patch always runs and the node shows a warning.
    /// </remarks>
    [ProcessNode(FragmentSelection = FragmentSelection.Explicit, Tags = "IsAssigned")]
    [Region(SupportedBorderControlPoints = ControlPointType.Accumulator)]
    public sealed class IfAssignedRegion<T> : IRegion<IIfAssignedInlay<T>>, IDisposable
    {
        internal const string AlwaysAssignedWarningMessage =
            "Values of this type are never null, so the patch inside always runs.";

        /// <summary>
        /// Whether <typeparamref name="T"/> is a non-nullable value type, whose values can't be null.
        /// </summary>
        internal static bool IsAlwaysAssigned { get; } =
            typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) is null;

        private readonly Dictionary<InputDescription, object?> _inputs = new();
        private readonly Dictionary<OutputDescription, object?> _outputs = new();
        private readonly Dictionary<string, object?> _accumulatorInputs = new();

        private Func<IIfAssignedInlay<T>>? _factory;
        private IIfAssignedInlay<T>? _inlay;
        private IDisposable? _alwaysAssignedWarning;
        private bool _disposed;

        [Fragment]
        public IfAssignedRegion(NodeContext nodeContext)
            : this(() => AddAlwaysAssignedWarning(nodeContext)) { }

        /// <summary>
        /// Test seam: allows to supply the way the warning for non-nullable value types is shown.
        /// </summary>
        internal IfAssignedRegion(Func<IDisposable>? addAlwaysAssignedWarning = null)
        {
            if (IsAlwaysAssigned && addAlwaysAssignedWarning is not null)
                _alwaysAssignedWarning = addAlwaysAssignedWarning();
        }

        private static IDisposable AddAlwaysAssignedWarning(NodeContext nodeContext)
        {
            // The runtime is resolved via AppHost.Current; ensure it is the app this node lives in.
            using var scope = nodeContext.AppHost.MakeCurrent();

            return nodeContext.AddPersistentMessage(
                MessageSeverity.Warning,
                AlwaysAssignedWarningMessage,
                includeAncestors: false
            );
        }

        void IRegion<IIfAssignedInlay<T>>.SetPatchInlayFactory(
            Func<IIfAssignedInlay<T>> patchInlayFactory
        )
        {
            ArgumentNullException.ThrowIfNull(patchInlayFactory);

            // Called again with a new factory after each recompile. The existing inlay is kept on
            // purpose: VL forwards calls on an outdated inlay to its hot-swapped version, state included.
            _factory = patchInlayFactory;
        }

        /// <summary>
        /// Executes the patch inside if <paramref name="input"/> is assigned.
        /// </summary>
        /// <param name="input">The value to check. Passed into the patch if assigned.</param>
        /// <param name="isAssigned">Whether the input was assigned and the patch got executed.</param>
        [Fragment]
        public void Update(T? input, out bool isAssigned)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            // Values written by the patch in a previous frame must not leak into a frame it didn't run in.
            _outputs.Clear();

            isAssigned = input is not null;
            if (input is null)
                return;

            var factory =
                _factory
                ?? throw new InvalidOperationException(
                    "Patch inlay factory has not been assigned."
                );

            _inlay ??= factory();
            _inlay.Update(input);
        }

        // Called for accumulators and for links crossing into the region.
        void IRegion<IIfAssignedInlay<T>>.AcknowledgeInput(
            in InputDescription description,
            object? outerValue
        )
        {
            _inputs[description] = outerValue;

            if (description.AccumulatorId is { } accumulatorId)
                _accumulatorInputs[accumulatorId] = outerValue;
        }

        void IRegion<IIfAssignedInlay<T>>.RetrieveInput(
            in InputDescription description,
            IIfAssignedInlay<T> patchInlay,
            out object? innerValue
        )
        {
            _inputs.TryGetValue(description, out innerValue);
        }

        void IRegion<IIfAssignedInlay<T>>.AcknowledgeOutput(
            in OutputDescription description,
            IIfAssignedInlay<T> patchInlay,
            object? innerValue
        )
        {
            _outputs[description] = innerValue;
        }

        // VL turns null (e.g. an accumulator without a connected input) into the default value of the output type.
        void IRegion<IIfAssignedInlay<T>>.RetrieveOutput(
            in OutputDescription description,
            out object? outerValue
        )
        {
            if (_outputs.TryGetValue(description, out outerValue))
                return;

            if (description.AccumulatorId is { } accumulatorId)
                _accumulatorInputs.TryGetValue(accumulatorId, out outerValue);
        }

        [Fragment]
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _alwaysAssignedWarning?.Dispose();
            _alwaysAssignedWarning = null;

            var inlay = _inlay;
            _inlay = null;
            (inlay as IDisposable)?.Dispose();

            _factory = null;
            _inputs.Clear();
            _outputs.Clear();
            _accumulatorInputs.Clear();
        }
    }
}
