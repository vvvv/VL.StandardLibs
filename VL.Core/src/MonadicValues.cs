#nullable enable
using System;
using System.Reflection;
using VL.Lib.IO;

namespace VL.Core
{
    /// <summary>
    /// Marks a type as monadic - it can wrap any basic type. The value wrapping is done through an intermediate as specified by the given factory.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public class MonadicAttribute : Attribute
    {
        public Type Factory { get; }

        /// <summary>
        /// An optional type filter. The type must have a default constructor and implement <see cref="IMonadicTypeFilter"/>.
        /// </summary>
        public Type? TypeFilter { get; }

        public MonadicAttribute(Type factory)
        {
            Factory = factory;
        }

        public MonadicAttribute(Type factory, Type typeFilter)
        {
            Factory = factory;
            TypeFilter = typeFilter;
        }
    }

    /// <summary>
    /// Allows to define what types the factory accepts.
    /// </summary>
    public interface IMonadicTypeFilter
    {
        bool Accepts(TypeDescriptor typeDescriptor);
    }

    /// <summary>
    /// Used in the <see cref="IMonadicTypeFilter.Accepts(TypeDescriptor)"/> method to decribe a type.
    /// </summary>
    /// <param name="Name">The name of the type as defined in VL.</param>
    /// <param name="Category">The category of the type as defined in VL.</param>
    /// <param name="IsImmutable">Whether or not the type is immutable.</param>
    /// <param name="IsValueType">Whether or not the type is a value type.</param>
    /// <param name="IsUnmanaged">Whether or not the type is an unmanaged (blittable) type.</param>
    /// <param name="ClrType">The dotnet runtime type. Can be null for patched types.</param>
    public record struct TypeDescriptor(
        string Name, 
        string Category, 
        bool IsImmutable, 
        bool IsValueType, 
        bool IsUnmanaged, 
        Type? ClrType)
    {
        public bool IsString => ClrType == typeof(string);
        public bool IsPath => ClrType == typeof(Path);
        public bool IsPrimitive => IsUnmanaged || IsString || IsPath;
    }

    /// <summary>
    /// Implementations must have a default constructor as well as a static readonly field called "Default".
    /// </summary>
    /// <typeparam name="TValue">The type of the value</typeparam>
    /// <typeparam name="TMonad">The type of the monadic value</typeparam>
    public interface IMonadicFactory<TValue, TMonad>
    {
        /// <summary>
        /// Creates a monad builder.
        /// </summary>
        /// <param name="isConstant">Whether or not the value is constant.</param>
        /// <returns>The monad builder</returns>
        IMonadBuilder<TValue, TMonad> GetMonadBuilder(bool isConstant);

        /// <summary>
        /// Creates a monad builder.
        /// </summary>
        /// <param name="isConstant">Whether or not the value is constant.</param>
        /// <param name="nodeContext">The node context - its path will have the id of the sink appended to it.</param>
        /// <returns>The monad builder</returns>
        IMonadBuilder<TValue, TMonad> GetMonadBuilder(bool isConstant, NodeContext nodeContext) => GetMonadBuilder(isConstant);
    }

    /// <summary>
    /// Builds the monadic value <typeparamref name="TMonad"/> out of <typeparamref name="TValue"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of the value</typeparam>
    /// <typeparam name="TMonad">The type of the monadic value</typeparam>
    public interface IMonadBuilder<TValue, TMonad>
    {
        TMonad Return(TValue value);

        /// <summary>
        /// Called when the system has no value yet. This is usually true for unconnected input pins.
        /// </summary>
        TMonad? Default() => default;
    }
}
