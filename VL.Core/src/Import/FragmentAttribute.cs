#nullable enable
using System;

namespace VL.Core.Import
{
    /// <summary>
    /// Marks a method or property to be included as a fragment in a process definition (<see cref="ProcessNodeAttribute"/>).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Constructor, AllowMultiple = false)]
    public sealed class FragmentAttribute : Attribute
    {
        /// <summary>
        /// Determines the order of the fragment.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Whether or not the fragment shall be hidden. Useful when the fragment selection is set to <see cref="FragmentSelection.Implicit"/> and certain members shall not be part of the process.
        /// </summary>
        public bool IsHidden { get; set; }
    }
}
