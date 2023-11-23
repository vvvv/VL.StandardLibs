#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using VL.Lib.Collections;

namespace VL.Core
{
    /// <summary>
    /// Interface to interact with VL types.
    /// </summary>
    public interface IVLTypeInfo
    {
        /// <summary>
        /// The name of the type.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The category of the type.
        /// </summary>
        string Category { get; }

        /// <summary>
        /// The full name of the type. For example "Integer32 [Primitive]" or "Spread [Collections] &lt;Float32 [Primitive]&gt;".
        /// </summary>
        string FullName { get; }

        UniqueId Id { get; }

        /// <summary>
        /// The CLR type.
        /// </summary>
        Type ClrType { get; }

        /// <summary>
        /// Whether or not this type is a patched VL type.
        /// </summary>
        bool IsPatched { get; }

        /// <summary>
        /// Whether or not this type is a VL class (mutable).
        /// </summary>
        bool IsClass { get; }

        /// <summary>
        /// Whether or not this type is a VL record (immutable).
        /// </summary>
        bool IsRecord { get; }

        /// <summary>
        /// Whether or not this type is immutable.
        /// </summary>
        bool IsImmutable { get; }

        /// <summary>
        /// Whether or not this type is an interface.
        /// </summary>
        bool IsInterface { get; }

        /// <summary>
        /// The user defined properties of this type.
        /// </summary>
        Spread<IVLPropertyInfo> Properties { get; }

        /// <summary>
        /// The user defined and system generated properties of this type.
        /// </summary>
        Spread<IVLPropertyInfo> AllProperties { get; }

        /// <summary>
        /// Returns the property with the given name.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <returns>The property or null.</returns>
        IVLPropertyInfo? GetProperty(string name);

        /// <summary>
        /// Returns a string representation of this type.
        /// </summary>
        /// <param name="includeCategory">Whether or not to include the category.</param>
        /// <returns>The string representation of this type.</returns>
        string ToString(bool includeCategory);

        /// <summary>
        /// Create a new instance of this type by calling it's constructor using default values for any of its arguments.
        /// If there's no default constructor registered for this type it will fallback to the default value.
        /// </summary>
        /// <param name="context">The node context to use. Used by patched types.</param>
        /// <returns>The new instance.</returns>
        object? CreateInstance(NodeContext context);

        /// <summary>
        /// Retrieves the default value of this type. 
        /// If there's no default value registered for this type it will return the CLR default.
        /// </summary>
        /// <returns>The default of this type.</returns>
        object? GetDefaultValue();

        /// <summary>
        /// Creates a generic type instance of out of this generic definition using the given type arguments.
        /// </summary>
        /// <param name="arguments">The type arguments to use for the type parameters of this definition.</param>
        /// <returns>The generic type instance.</returns>
        IVLTypeInfo MakeGenericType(IReadOnlyList<IVLTypeInfo> arguments);
    }
}
