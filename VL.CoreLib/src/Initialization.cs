using Stride.Core.Mathematics;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Reactive.Disposables;
using VL.Core;
using VL.Core.CompilerServices;
using VL.Core.Reactive;

[assembly: AssemblyInitializer(typeof(VL.Lib.VL_CoreLib_Initializer))]

namespace VL.Lib
{
    public sealed class VL_CoreLib_Initializer : AssemblyInitializer<VL_CoreLib_Initializer>
    {
        static VL_CoreLib_Initializer()
        {
            // Add type converter for Stride.Core.Mathematics vectors - allows to convert the value of an IO box when changing its type
            TypeDescriptor.AddAttributes(typeof(float), new TypeConverterAttribute(typeof(SingleConverter)));
            TypeDescriptor.AddAttributes(typeof(double), new TypeConverterAttribute(typeof(DoubleConverter)));
            TypeDescriptor.AddAttributes(typeof(Vector2), new TypeConverterAttribute(typeof(Vector2Converter)));
            TypeDescriptor.AddAttributes(typeof(Vector3), new TypeConverterAttribute(typeof(Vector3Converter)));
            TypeDescriptor.AddAttributes(typeof(Vector4), new TypeConverterAttribute(typeof(Vector4Converter)));
        }

        protected override void RegisterServices(IVLFactory factory)
        {
            Mathematics.Serialization.RegisterSerializers(factory);

            // did we get called by runtime?
            if (ServiceRegistry.IsCurrent())
            {
                var appHost = ServiceRegistry.Current.GetService<IAppHost>();
                if (appHost != null)
                {
                    // the following line forces the creation of a channel hub per entry point! HDE code assumes that there is one.
                    var hubForApp = ((ChannelHub)IChannelHub.HubForApp);
                    hubForApp.AppName = appHost.AppName;
                    var basePath = PathProviderUtils.GetApplicationBasePath();
                    hubForApp.AppBasePath = basePath;
                    var watcher = ChannelHubConfigWatcher.FromApplicationBasePath(basePath);
                    // make sure all channels of config exist in app-channelhub.
                    if (watcher != null)
                        ((ChannelHub)IChannelHub.HubForApp).MustHaveDescriptive = watcher.Descriptions;
                }
            }

            // registering node factory producing nodes for global channels is necessary in any case.
            // compiler needs it to output correct code. target code calls into the nodes. So they need to be present as well.
            ChannelHubNodeBuilding.RegisterChannelHubNodeFactoryTriggeredViaConfigFile(factory);
        }

        class SingleConverter : System.ComponentModel.SingleConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                if (sourceType == typeof(Vector2) || sourceType == typeof(Vector3) || sourceType == typeof(Vector4))
                    return true;
                return base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is Vector2)
                {
                    var vector = (Vector2)value;
                    return vector.X;
                }
                if (value is Vector3)
                {
                    var vector = (Vector3)value;
                    return vector.X;
                }
                if (value is Vector4)
                {
                    var vector = (Vector4)value;
                    return vector.X;
                }
                return base.ConvertFrom(context, culture, value);
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                if (destinationType == typeof(Vector2) || destinationType == typeof(Vector3) || destinationType == typeof(Vector4))
                    return true;
                return base.CanConvertTo(context, destinationType);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                var singleValue = (float)value;
                if (destinationType == typeof(Vector2))
                    return new Vector2(singleValue);
                if (destinationType == typeof(Vector3))
                    return new Vector3(singleValue);
                if (destinationType == typeof(Vector4))
                    return new Vector4(singleValue);
                return base.ConvertTo(context, culture, value, destinationType);
            }
        }

        class DoubleConverter : System.ComponentModel.DoubleConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                if (sourceType == typeof(Vector2) || sourceType == typeof(Vector3) || sourceType == typeof(Vector4))
                    return true;
                return base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is Vector2)
                {
                    var vector = (Vector2)value;
                    return (double)vector.X;
                }
                if (value is Vector3)
                {
                    var vector = (Vector3)value;
                    return (double)vector.X;
                }
                if (value is Vector4)
                {
                    var vector = (Vector4)value;
                    return (double)vector.X;
                }
                return base.ConvertFrom(context, culture, value);
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                if (destinationType == typeof(Vector2) || destinationType == typeof(Vector3) || destinationType == typeof(Vector4))
                    return true;
                return base.CanConvertTo(context, destinationType);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                var singleValue = (float)Convert.ChangeType(value, typeof(float));
                if (destinationType == typeof(Vector2))
                    return new Vector2(singleValue);
                if (destinationType == typeof(Vector3))
                    return new Vector3(singleValue);
                if (destinationType == typeof(Vector4))
                    return new Vector4(singleValue);
                return base.ConvertTo(context, culture, value, destinationType);
            }
        }

        class Vector2Converter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                if (sourceType == typeof(float) || sourceType == typeof(Vector3) || sourceType == typeof(Vector4))
                    return true;
                return base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is float)
                {
                    var singleValue = (float)value;
                    return new Vector2(singleValue);
                }
                if (value is Vector3)
                {
                    var vector = (Vector3)value;
                    return new Vector2(vector.X, vector.Y);
                }
                if (value is Vector4)
                {
                    var vector = (Vector4)value;
                    return new Vector2(vector.X, vector.Y);
                }
                return base.ConvertFrom(context, culture, value);
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                if (destinationType == typeof(float) || destinationType == typeof(Vector3) || destinationType == typeof(Vector4))
                    return true;
                return base.CanConvertTo(context, destinationType);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                var vector = (Vector2)value;
                if (destinationType == typeof(float))
                    return vector.X;
                if (destinationType == typeof(Vector3))
                    return new Vector3(vector, 0f);
                if (destinationType == typeof(Vector4))
                    return new Vector4(vector, 0f, 1f);
                return base.ConvertTo(context, culture, value, destinationType);
            }
        }

        class Vector3Converter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                if (sourceType == typeof(float) || sourceType == typeof(Vector2) || sourceType == typeof(Vector4))
                    return true;
                return base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is float)
                {
                    var singleValue = (float)value;
                    return new Vector3(singleValue);
                }
                if (value is Vector2)
                {
                    var vector = (Vector2)value;
                    return new Vector3(vector.X, vector.Y, 0f);
                }
                if (value is Vector4)
                {
                    var vector = (Vector4)value;
                    return new Vector3(vector.X, vector.Y, 0f);
                }
                return base.ConvertFrom(context, culture, value);
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                if (destinationType == typeof(float) || destinationType == typeof(Vector2) || destinationType == typeof(Vector4))
                    return true;
                return base.CanConvertTo(context, destinationType);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                var vector = (Vector3)value;
                if (destinationType == typeof(float))
                    return vector.X;
                if (destinationType == typeof(Vector2))
                    return new Vector2(vector.X, vector.Y);
                if (destinationType == typeof(Vector4))
                    return new Vector4(vector.X, vector.Y, vector.Z, 1f);
                return base.ConvertTo(context, culture, value, destinationType);
            }
        }

        class Vector4Converter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                if (sourceType == typeof(float) || sourceType == typeof(Vector2) || sourceType == typeof(Vector3))
                    return true;
                return base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is float)
                {
                    var singleValue = (float)value;
                    return new Vector4(singleValue);
                }
                if (value is Vector2)
                {
                    var vector = (Vector2)value;
                    return new Vector4(vector.X, vector.Y, 0f, 1f);
                }
                if (value is Vector3)
                {
                    var vector = (Vector3)value;
                    return new Vector4(vector.X, vector.Y, vector.Z, 1f);
                }
                return base.ConvertFrom(context, culture, value);
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                if (destinationType == typeof(float) || destinationType == typeof(Vector2) || destinationType == typeof(Vector3))
                    return true;
                return base.CanConvertTo(context, destinationType);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                var vector = (Vector4)value;
                if (destinationType == typeof(float))
                    return vector.X;
                if (destinationType == typeof(Vector2))
                    return new Vector2(vector.X, vector.Y);
                if (destinationType == typeof(Vector3))
                    return new Vector3(vector.X, vector.Y, vector.Z);
                return base.ConvertTo(context, culture, value, destinationType);
            }
        }
    }
}
