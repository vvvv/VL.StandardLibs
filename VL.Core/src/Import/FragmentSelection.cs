#nullable enable

namespace VL.Core.Import
{
    /// <summary>
    /// Allows to specifify what members of a type get selected to form a process its fragments.
    /// </summary>
    public enum FragmentSelection
    {
        /// <summary>
        /// All public methods and properties of the type will be included.
        /// </summary>
        Implicit,
        /// <summary>
        /// Only public methods and properties with the <see cref="FragmentAttribute"/> will be included.
        /// </summary>
        Explicit
    }
}
