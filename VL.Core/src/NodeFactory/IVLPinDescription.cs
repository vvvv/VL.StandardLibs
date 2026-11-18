#nullable enable
using System;
using VL.Model;

namespace VL.Core
{
    /// <summary>
    /// Describes a pin of a node: its name, type and default value, and whether it is a pin group.
    /// </summary>
    public interface IVLPinDescription
    {
        /// <summary>
        /// The name of the pin as shown on the node.
        /// </summary>
        string Name { get; }
        /// <summary>
        /// The type of the values flowing through the pin. For a pin group this is the collection type (see <see cref="PinGroupKind"/>).
        /// </summary>
        Type Type { get; }
        /// <summary>
        /// The value the pin has when created, or null if there is none.
        /// </summary>
        object? DefaultValue { get; }
        /// <summary>
        /// Whether this pin is a pin group (a pin the user can add or remove instances of), and of which kind. <see cref="Model.PinGroupKind.None"/> for a regular pin.
        /// </summary>
        PinGroupKind PinGroupKind => PinGroupKind.None;
        /// <summary>
        /// The number of pins a new pin group initially has. Only relevant if <see cref="PinGroupKind"/> is not <see cref="Model.PinGroupKind.None"/>.
        /// </summary>
        int PinGroupDefaultCount => 0;
        /// <summary>
        /// How the user is allowed to edit the pin group, or null for the default. Only relevant if <see cref="PinGroupKind"/> is not <see cref="Model.PinGroupKind.None"/>.
        /// </summary>
        PinGroupEditModes? PinGroupEditMode => null;
    }
}
#nullable restore