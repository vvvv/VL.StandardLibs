#nullable enable
using System;

namespace VL.Core.Import;

/// <summary>
/// Use on an input pin of type string to tell the editor to show the value of that pin as the label of the node instead of its name.
/// The node keeps its name when the pin is linked or its value is empty.
/// The editor shows a node with such a label a bit different from other nodes (in a monospace font, like code), as the user can't find it by that label in the node browser.
/// An optional pin with this attribute is visible on new nodes, it can then be hidden per node.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class NodeLabelAttribute : Attribute
{
}
