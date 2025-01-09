﻿#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using VL.Lib.IO;

namespace VL.Core
{
    /// <summary>
    /// Allows to specify a type filter for a <see cref="IMonadicValue{TValue}"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public class MonadicTypeFilterAttribute : Attribute
    {
        /// <summary>
        /// The type must have a default constructor and implement <see cref="IMonadicTypeFilter"/>.
        /// </summary>
        public Type TypeFilter { get; }

        public MonadicTypeFilterAttribute(Type typeFilter)
        {
            TypeFilter = typeFilter;
        }
    }

    /// <summary>
    /// Defines what types shall be accepted by a <see cref="IMonadicValue{TValue}"/>.
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
    /// Non-generic marker interface. Do not implement directly.
    /// </summary>
    public interface IMonadicValue
    {        
        /// <summary>
        /// Whether or not a value is available.
        /// </summary>
        bool HasValue { get; }

        /// <summary>
        /// Whether or not a value can be assigned to this instance.
        /// </summary>
        bool AcceptsValue { get; }

        /// <summary>
        /// The stored value as object.
        /// </summary>
        object? BoxedValue { get; }
    }

    /// <summary>
    /// Tells VL that connections from <typeparamref name="TValue"/> to the type implementing this interface are allowed.
    /// </summary>
    public interface IMonadicValue<TValue> : IMonadicValue
    {
        /// <summary>
        /// Creates a new instance.
        /// </summary>
        static abstract IMonadicValue<TValue> Create(NodeContext nodeContext, TValue? value);

        /// <summary>
        /// Whether or not the default value is not NULL. This is a hint for UIs to not display the checkbox.
        /// </summary>
        static virtual bool HasCustomDefault => false;

        static virtual IMonadicValue<TValue>? Default => default;

        /// <summary>
        /// The stored value.
        /// </summary>
        TValue? Value { get; }

        /// <summary>
        /// Sets a new value and potentially returns a new instance.
        /// </summary>
        IMonadicValue<TValue> SetValue(TValue? value);

        object? IMonadicValue.BoxedValue => Value;
    }

    /// <summary>
    /// Utility interface to share common editor implementations for <see cref="Nullable{T}"/>, <see cref="Optional{T}"/> and <see cref="IMonadicValue{TValue}"/>.
    /// </summary>
    internal interface IMonadicValueEditor<TValue, TMonad>
    {
        TMonad Create(TValue? value);
        bool HasValue([NotNullWhen(true)] TMonad? monad);
        TValue? GetValue(TMonad monad);
        TMonad SetValue(TMonad monad, TValue? value);
        bool HasCustomDefault => false;
    }





















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
