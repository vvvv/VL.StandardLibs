using System;

namespace VL.Core.Import;

/// <summary>
/// Use on an input pin of type string to tell the editor to show the value of that pin as the label of the node instead of its name.
/// The node keeps its name when the pin is linked or its value is empty.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class NodeLabelAttribute : Attribute
{
}
