using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using VL.Core;
using VL.Core.CompilerServices;
using VL.Lib.Collections;
using TypeDescriptor = System.ComponentModel.TypeDescriptor;

[assembly: AssemblyInitializer(typeof(VL.Lib.VL_Core_Initializer))]

namespace VL.Lib
{
    public sealed class VL_Core_Initializer : AssemblyInitializer<VL_Core_Initializer>
    {
        static VL_Core_Initializer()
        {
            TypeDescriptor.AddAttributes(typeof(IO.Path), new TypeConverterAttribute(typeof(PathConverter)));
            TypeDescriptor.AddAttributes(typeof(char), new TypeConverterAttribute(typeof(CharConverter)));
            TypeDescriptor.AddAttributes(typeof(ICollection), new TypeConverterAttribute(typeof(CollectionConverter)));
        }

        protected override void RegisterServices(IVLFactory factory)
        {
        }

        class PathConverter : System.ComponentModel.StringConverter
        {   
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                if (sourceType == typeof(Lib.IO.Path))
                    return true;
                return base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is string)
                {
                    if (value != null)
                        return new IO.Path((string)value);
                }
                return base.ConvertFrom(context, culture, value);
            }
        }

        class CharConverter : System.ComponentModel.CharConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                if (sourceType == typeof(string))
                    return true;
                return base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is string)
                {
                    var s = value as string;
                    if (!string.IsNullOrEmpty(s))
                        return s[0];
                    return default(char);
                }
                return base.ConvertFrom(context, culture, value);
            }
        }

        class CollectionConverter : System.ComponentModel.CollectionConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                // We can always put a single value into a spread
                if (!typeof(ICollection).IsAssignableFrom(sourceType))
                    return true;
                return base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value != null && !(value is ICollection))
                {
                    var elementType = value.GetType();
                    var builder = GetSpreadBuilder(elementType);
                    builder.Add(value);
                    return ToSpread(builder);
                }
                return base.ConvertFrom(context, culture, value);
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                if (destinationType.IsGenericType && destinationType.GetGenericTypeDefinition() == typeof(Spread<>))
                    return true;
                return base.CanConvertTo(context, destinationType);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                var collection = value as ICollection;
                if (destinationType.IsGenericType && destinationType.GetGenericTypeDefinition() == typeof(Spread<>))
                {
                    var elementType = destinationType.GenericTypeArguments[0];
                    var builder = GetSpreadBuilder(elementType);
                    foreach (var item in collection)
                        builder.Add(item);
                    return ToSpread(builder);
                }
                return base.ConvertTo(context, culture, value, destinationType);
            }

            private static IList GetSpreadBuilder(Type elementType)
            {
                var method = typeof(Spread).GetMethod(nameof(Spread.CreateBuilder), Array.Empty<Type>()).MakeGenericMethod(elementType);
                return method.Invoke(null, Array.Empty<object>()) as IList;
            }

            private static object ToSpread(IList builder)
            {
                var method = builder.GetType().GetMethod(nameof(SpreadBuilder<object>.ToSpread));
                return method.Invoke(builder, Array.Empty<object>());
            }
        }
    }
}
