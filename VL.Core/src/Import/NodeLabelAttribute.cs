#nullable enable
using System;

namespace VL.Core.Import;

/// <summary>
/// Use on an input pin of type string to tell the editor to show the value of that pin as the label of the node instead of its name.
/// The node keeps its name when the pin is linked or its value is empty.
/// An optional pin with this attribute is visible on new nodes, it can then be hidden per node.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class NodeLabelAttribute : Attribute
{
    /// <summary>
    /// A text shown in front of the value.
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// A text shown behind the value.
    /// </summary>
    public string? Suffix { get; set; }
}
