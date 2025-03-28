#nullable enable
using System;
using System.Collections.Generic;

namespace VL.Core.Import;

/// <summary>
/// Abstract base class for creating process node factories.
/// </summary>
public abstract class ProcessNodeFactory
{
    /// <summary>
    /// Represents a node with its associated <see cref="ProcessNodeAttribute"/>.
    /// </summary>
    /// <param name="Attribute">The attribute defining the process node.</param>
    /// <param name="PrivateData">Optional private data associated with the node.</param>
    public record struct Node(ProcessNodeAttribute Attribute, string? PrivateData = null);

    /// <summary>
    /// Gets the collection of nodes defined by this factory.
    /// </summary>
    /// <returns>An enumerable collection of nodes.</returns>
    public virtual IEnumerable<Node> GetNodes() => Array.Empty<Node>();

    /// <summary>
    /// Gets the collection of nodes defined by this factory for a specific path.
    /// </summary>
    /// <param name="path">The path for which to get the nodes.</param>
    /// <returns>An enumerable collection of nodes for the specified path.</returns>
    public virtual IEnumerable<Node> GetNodesForPath(string path) => Array.Empty<Node>();
}
