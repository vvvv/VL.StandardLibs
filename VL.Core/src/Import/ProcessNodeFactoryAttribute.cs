#nullable enable
using System;

namespace VL.Core.Import
{
    /// <summary>
    /// Dynamic version of <see cref="ProcessNodeAttribute"/> that allows to specify the factory type that creates the node attributes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ProcessNodeFactoryAttribute : Attribute
    {
        /// <summary>
        /// Creates a new instance of <see cref="ProcessNodeFactoryAttribute"/>.
        /// </summary>
        /// <param name="factoryType">The type of the factory that creates the node attributes. Must derive from <see cref="ProcessNodeFactory"/>.</param>
        public ProcessNodeFactoryAttribute(Type factoryType)
        {
            FactoryType = factoryType;
        }

        /// <summary>
        /// The type of the factory that creates the node attributes. Must derive from <see cref="ProcessNodeFactory"/>.
        /// </summary>
        public Type FactoryType { get; }

        /// <summary>
        /// Whether or not the class and its members shall be imported.
        /// Set to true if any of the generated nodes have their state output enabled <see cref="ProcessNodeAttribute.HasStateOutput"/>.
        /// </summary>
        public bool ImportClass { get; set; }

        internal ProcessNodeFactory CreateFactory()
        {
            return (ProcessNodeFactory?)Activator.CreateInstance(FactoryType, nonPublic: true) ?? throw new InvalidOperationException($"Could not create instance of {FactoryType}");
        }
    }
}
