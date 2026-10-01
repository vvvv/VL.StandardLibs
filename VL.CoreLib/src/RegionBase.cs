#nullable enable
using System;
using System.Collections.Generic;
using VL.Core.PublicAPI;

namespace VL.Lib.CustomRegions;

/// <summary>
/// Provides the standard storage and <see cref="IRegion{TInlayFactory, TInlay}"/> implementation for a custom region.
/// </summary>
/// <typeparam name="TInlayFactory">A delegate type whose return type is <typeparamref name="TInlay"/>. The compiler uses its parameters as the region's Create inputs.</typeparam>
/// <typeparam name="TInlay">The interface or class implemented by the generated patch inlay.</typeparam>
/// <remarks>
/// Derive from this class and pass the same factory and inlay types used by <see cref="IRegion{TInlayFactory, TInlay}"/>.
/// The region compiler requires the factory delegate to return <typeparamref name="TInlay"/> and generates its Create pins from the delegate parameters.
/// <para>
/// The default implementation stores acknowledged inputs and outputs by their descriptions. Retrieved values are null when no value has been stored.
/// Override the virtual methods to customize factory assignment, value storage, or retrieval.
/// </para>
/// </remarks>
public abstract class RegionBase<TInlayFactory, TInlay> : IRegion<TInlayFactory, TInlay>
    where TInlayFactory : Delegate
{
    private readonly Dictionary<InputDescription, object?> inputs = new();
    private readonly Dictionary<OutputDescription, object?> outputs = new();

    /// <summary>
    /// Gets the factory assigned by the region compiler.
    /// </summary>
    /// <remarks>
    /// The factory is assigned through <see cref="SetPatchInlayFactory(TInlayFactory)"/> before the region uses it.
    /// </remarks>
    protected TInlayFactory PatchInlayFactory { get; private set; } = default!;

    /// <summary>
    /// Stores the factory provided by the region compiler.
    /// </summary>
    /// <param name="factory">The factory delegate used to create patch inlays.</param>
    public virtual void SetPatchInlayFactory(TInlayFactory factory)
    {
        PatchInlayFactory = factory;
    }

    /// <summary>
    /// Stores an outer input value under its description.
    /// </summary>
    /// <param name="description">The input control point or crossing link.</param>
    /// <param name="outerValue">The value received by the region.</param>
    public virtual void AcknowledgeInput(in InputDescription description, object? outerValue)
    {
        inputs[description] = outerValue;
    }

    /// <summary>
    /// Retrieves the most recently stored outer output value for a description.
    /// </summary>
    /// <param name="description">The output control point.</param>
    /// <param name="outerValue">The stored value, or null if no value has been acknowledged.</param>
    public virtual void RetrieveOutput(in OutputDescription description, out object? outerValue)
    {
        outputs.TryGetValue(description, out outerValue);
    }

    /// <summary>
    /// Retrieves the stored input value for a patch inlay.
    /// </summary>
    /// <param name="description">The input control point or crossing link.</param>
    /// <param name="patchInlay">The patch inlay requesting the value.</param>
    /// <param name="innerValue">The stored value, or null if no value has been acknowledged.</param>
    public virtual void RetrieveInput(in InputDescription description, TInlay patchInlay, out object? innerValue)
    {
        inputs.TryGetValue(description, out innerValue);
    }

    /// <summary>
    /// Stores an output value produced by a patch inlay under its description.
    /// </summary>
    /// <param name="description">The output control point.</param>
    /// <param name="patchInlay">The patch inlay producing the value.</param>
    /// <param name="innerValue">The output value produced inside the region.</param>
    public virtual void AcknowledgeOutput(in OutputDescription description, TInlay patchInlay, object? innerValue)
    {
        outputs[description] = innerValue;
    }
}