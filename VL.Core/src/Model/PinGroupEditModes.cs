using System;
using System.ComponentModel;
using VL.Core;

namespace VL.Model;

/// <summary>
/// Specifies the different modes for editing pin groups in a node.
/// </summary>
[Flags]
public enum PinGroupEditModes
{
    /// <summary>
    /// Pin group can not be modified directly. Useful when pins get added or removed by the node itself.
    /// </summary>
    None = 0,

    /// <summary>
    /// The user can only add pins.
    /// </summary>
    /// <remarks>
    /// There's currently no action which supports this. Therefore we hide it from the user.
    /// </remarks>
    [Browsable(false)]
    AddOnly = 1,

    /// <summary>
    /// The user can only remove pins. This mode is supported by the node inspector.
    /// </summary>
    /// <remarks>
    /// Useful for example when pins get added through other means (like the <see cref="IHasLearnMode"/>) but user shall be able to remove them.
    /// </remarks>
    RemoveOnly = 2,

    /// <summary>
    /// The user can add and remove pins freely (for example via Ctrl++ and Ctrl+-).
    /// </summary>
    AddAndRemove = AddOnly | RemoveOnly,
}
