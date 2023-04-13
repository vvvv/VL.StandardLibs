using System;
using System.Reflection;

namespace VL.Core
{
    /// <summary>
    /// Marks a type as monadic - it can wrap any basic type. The value wrapping is done through an intermediate as specified by the given factory.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public class MonadicAttribute : Attribute
    {
        public Type Factory { get; }

        public MonadicAttribute(Type factory)
        {
            Factory = factory;
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
        TMonad Default(TValue defaultValue) => Return(defaultValue);
    }

    public static class MonadicUtils
    {
        public static bool IsMonadicType(this Type type) => type.GetCustomAttributeSafe<MonadicAttribute>() != null;

        public static Type GetMonadicFactoryType(this Type monadicType, Type valueType)
        {
            var attribute = monadicType.GetCustomAttributeSafe<MonadicAttribute>();
            if (attribute is null)
                return null;

            return attribute.Factory?.MakeGenericType(valueType);
        }

        public static IMonadicFactory<TValue, TMonad> GetMonadicFactory<TValue, TMonad>(this Type monadicType)
        {
            var factoryType = monadicType.GetMonadicFactoryType(typeof(TValue));
            if (factoryType is null)
                return null;

            return Activator.CreateInstance(factoryType) as IMonadicFactory<TValue, TMonad>;
        }
    }
}
