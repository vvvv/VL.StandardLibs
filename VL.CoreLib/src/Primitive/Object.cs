using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Core;

namespace VL.Lib.Primitive.Object
{
    public static class ObjectHelpers
    {
        public static T NULL<T>() // why no class constraint?
        {
            var result = default(T);
            if (result != null)
                throw new Exception("The type " + typeof(T).Name + " is not a nullable type.");

            return result;
        }

        /// <summary>
        /// NULL in case of reference type, unitialized in case of value type, e.g. (0,0,0,0) for a vector4 
        /// </summary>
        public static T NULL_OR_DEFAULT<T>() => default;

        /// <summary>
        /// Whether or not the value is null
        /// </summary>
        /// <param name="x"></param>
        /// <param name="result"></param>
        /// <param name="notAssigned"></param>
        public static void IsAssigned(object x, out bool result, out bool notAssigned)
        {
            result = x != null;
            notAssigned = !result;
        }

        /// <summary>
        /// The ?? operator is called the null-coalescing operator. It returns the left-hand operand if the operand is not null; otherwise it returns the right hand operand
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static T NullCoalescing<T>(T x, T y)
        {
            return x != null ? x : y;
        }

        /// <summary>
        /// If the Input is NULL the fallbackValue will be returned
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T AvoidNULL<T>(T input, T fallbackValue)
        {
            return input != null ? input : fallbackValue;
        }

        /// <summary>
        /// Casts the input value to the downstream connected type. Will throw a InvalidCastException if the cast fails
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public static T HardCast<T>(object input)
        {
            return IHotswapSpecificNodes.Impl.HardCast<T>(input);
        }


        /// <summary>
        /// Casts the input value to the downstream connected type. In case the cast fails the provided default value will be used and the success output will return false
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="default"></param>
        /// <param name="result"></param>
        /// <param name="success"></param>
        public static void CastAs<T>(object input, T @default, out T result, out bool success)
        {
            IHotswapSpecificNodes.Impl.CastAs(input, @default, out result, out success);
        }

        /// <summary>
        /// Casts the input value to the downstream connected type. In case the cast fails the result will be unassigned or unitialized in case of a value type 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="result"></param>
        /// <param name="success"></param>
        public static void CastAs_Slim<T>(object input, out T result, out bool success)
        {
            IHotswapSpecificNodes.Impl.CastAs(input, default, out result, out success);
        }

        /// <summary>
        /// Casts the input value to the downstream connected type. In case the cast fails the provided default value will be used and the success output will return false
        /// </summary>
        /// <typeparam name="TIn"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="input"></param>
        /// <param name="default"></param>
        /// <param name="result"></param>
        /// <param name="success"></param>
        public static void CastAsGeneric<TIn, TOut>(TIn input, TOut @default, out TOut result, out bool success)
        {
            IHotswapSpecificNodes.Impl.CastAsGeneric(input, @default, out result, out success);
        }
        /// <summary>
        /// Helps in terms of getting your patch generic.  
        /// </summary>
        /// <typeparam name="TIn"></typeparam>
        /// <param name="input"></param>
        /// <param name="result"></param>
        public static void AsObject<TIn>(TIn input, out object result)       
        {
            result = input;
        }

        /// <summary>
        /// Returns the Type Object for the type that got inferred at compile time in order to do some tricks at runtime.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input">The object flowing at runtime is irrelevant and thus can be NULL. Use type annotations in order to set the type. If you actually have an object at runtime use the GetType node instead.</param>
        /// <returns></returns>
        public static Type TypeOf<T>(T input)
            => typeof(T);

        /// <summary>
        /// Returns the input value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="x"></param>
        /// <returns></returns>
        public static T Identity<T>(this T x)
        {
            return x;
        }

        public static void ShareInstance<T>(T instance, out T output1, out T output2)
        {
            output1 = instance;
            output2 = instance;
        }

        /// <summary>
        /// Calls the virtual Equals method on the input value
        /// </summary>
        /// <param name="input"></param>
        /// <param name="input2"></param>
        /// <returns></returns>
        public static bool Eq(this object input, object input2)
        {
            return Equals(input, input2);
        }

        /// <summary>
        /// Calls the virtual ToString method on the input value
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string ToString(object input)
        {
            return input?.ToString();
        }

        //// Implementation for type class Show which all .NET types are a member of.
        //// We can't use the Object.ToString method directly as it has a different
        //// resulting node signature as ValueType.ToString:
        //// Object.ToString has a state output whereas ValueType.ToString has not.
        //public static string ToString(this object x)
        //{
        //    if (x != null)
        //        return x.ToString();
        //    else
        //        return "";
        //}
    }
}
