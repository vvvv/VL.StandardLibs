#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using VL.Lib.IO;

namespace VL.Core
{
    /// <summary>
    /// Allows to specify a type filter 
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
    /// Allows to define what types shall be accepted by a <see cref="IMonadicValue{TValue}"/>.
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

    public interface IMonadicValue
    {
        static abstract IMonadicValue Create(NodeContext nodeContext);
        static virtual bool DefaultIsNullOrNoValue => true;
    }

    public interface IMonadicValue<TValue> : IMonadicValue
    {
        bool HasValue => true;
        bool AcceptsValue => true;
        TValue? Value { get; set; }
    }

    public interface IMonadicValueEditor<TValue, TMonad>
    {
        TMonad Create(TValue? value);
        bool HasValue([NotNullWhen(true)] TMonad? monad);
        TValue? GetValue(TMonad monad);
        TMonad SetValue(TMonad monad, TValue? value);
        bool DefaultIsNullOrNoValue => true;
    }
}
