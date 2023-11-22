#nullable enable
using System;

namespace VL.Core.Import
{
    /// <summary>
    /// Defines a process node. The class must have a public constructor.
    /// 
    /// By default all the public methods and properties are used as fragments.
    /// Fragments can be selected individually via the <see cref="FragmentSelection"/> property.
    /// 
    /// The default ordering of the fragments is: setters, methods, getters with the inner order determined by the declaration order.
    /// 
    /// Members of base classes are only included if the base class also has the same attribute.
    /// Overriding a member will not change the order of the fragment.
    /// </summary>
    /// <remarks>
    /// Note: If set on a class all its members will no longer show up as individual nodes, except the process defines a state output
    /// via the <see cref="HasStateOutput"/> property.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ProcessNodeAttribute : Attribute
    {
        /// <summary>
        /// The name of the process. Leave empty to use the class name.
        /// </summary>
        public string? Name { get; set; }
        /// <summary>
        /// Whether or not the process shall have a state output. If true, the class its members will also be available as individual nodes.
        /// </summary>
        public bool HasStateOutput { get; set; }
        /// <summary>
        /// Controls how fragments get selected. By default all public members will be included.
        /// </summary>
        public FragmentSelection FragmentSelection { get; set; }
    }
}
