using Stride.Core;
using Stride.Core.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace VL.Stride
{
    static class Helper
    {
        static readonly Regex FSpaceAndCharRegex = new Regex(" [a-zA-Z]", RegexOptions.Compiled);
        static readonly Regex FLowerAndUpperRegex = new Regex("[a-z0-9][A-Z0-9]", RegexOptions.Compiled);

        public static string UpperCaseAfterSpace(this string name)
        {
            return FSpaceAndCharRegex.Replace(name, m => $" {char.ToUpper(m.Value[1])}");
        }

        public static string InsertSpaces(this string name)
        {
            return FLowerAndUpperRegex.Replace(name, m => $"{m.Value[0]} {m.Value[1]}");
        }

        public static Func<TInstance, TValue> BuildGetter<TInstance, TValue>(this MemberInfo member)
        {
            if (member is PropertyInfo p)
                return BuildPropertyGetter<TInstance, TValue>(p);
            else if (member is FieldInfo f)
                return BuildFieldGetter<TInstance, TValue>(f);
            else
                throw new NotImplementedException();
        }

        public static Action<TInstance, TValue> BuildSetter<TInstance, TValue>(this MemberInfo member)
        {
            if (member is PropertyInfo p)
                return BuildPropertySetter<TInstance, TValue>(p);
            else if (member is FieldInfo f)
                return BuildFieldSetter<TInstance, TValue>(f);
            else
                throw new NotImplementedException();
        }

        public static Func<TInstance, TValue> BuildPropertyGetter<TInstance, TValue>(PropertyInfo member)
        {
            return (Func<TInstance, TValue>)member.GetGetMethod().CreateDelegate(typeof(Func<TInstance, TValue>));
        }

        public static Action<TInstance, TValue> BuildPropertySetter<TInstance, TValue>(PropertyInfo member)
        {
            return (Action<TInstance, TValue>)member.GetSetMethod().CreateDelegate(typeof(Action<TInstance, TValue>));
        }

        public static Func<TInstance, TValue> BuildFieldGetter<TInstance, TValue>(FieldInfo member)
        {
            var targetExp = Expression.Parameter(typeof(TInstance), "instance");

            var fieldExp = Expression.Field(targetExp, member);

            return Expression.Lambda<Func<TInstance, TValue>>(fieldExp, targetExp).Compile();
        }

        public static Action<TInstance, TValue> BuildFieldSetter<TInstance, TValue>(FieldInfo member)
        {
            var targetExp = Expression.Parameter(typeof(TInstance), "instance");
            var valueExp = Expression.Parameter(typeof(TValue), "value");

            var fieldExp = Expression.Field(targetExp, member);
            var assignExp = Expression.Assign(fieldExp, valueExp);

            return Expression.Lambda<Action<TInstance, TValue>>(assignExp, targetExp, valueExp).Compile();
        }

        

        public static Type GetPropertyType(this MemberInfo member)
        {
            if (member is PropertyInfo p)
                return p.PropertyType;
            else if (member is FieldInfo f)
                return f.FieldType;
            else
                throw new NotImplementedException();
        }

        public static IEnumerable<(MemberInfo Property, int? Order, string Name, string Category)> GetStrideProperties(this Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetMethod != null && p.GetMethod.IsPublic && ((p.SetMethod != null && p.SetMethod.IsPublic) || !p.PropertyType.IsValueType))
                .OfType<MemberInfo>()
                .Concat(type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                .Select(p =>
                {
                    // Do not include properties which have explicit ignore flags set
                    if (p.GetCustomAttribute<DataMemberIgnoreAttribute>() != null)
                        return default;

                    var display = p.GetCustomAttribute<DisplayAttribute>();
                    if (display != null && !display.Browsable)
                        return default;

                    // At least one of the following attributes must be set
                    var dataMember = p.GetCustomAttribute<DataMemberAttribute>();
                    var dataMemberRange = p.GetCustomAttribute<DataMemberRangeAttribute>();
                    var customSerializer = p.GetCustomAttribute<DataMemberCustomSerializerAttribute>();
                    if (display is null && dataMember is null && dataMemberRange is null && customSerializer is null)
                        return default;

                    var name = display?.Name?.UpperCaseAfterSpace() ?? p.Name.InsertSpaces();
                    var order = dataMember?.Order;

                    // Enabled pin always comes last
                    if (name == "Enabled")
                        order = int.MaxValue;

                    return (
                        Property: p,
                        Order: order,
                        Name: name,
                        Category: display?.Category?.UpperCaseAfterSpace());
                })
                .Where(p => p != default);
        }
    }
}
