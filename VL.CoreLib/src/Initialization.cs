using Microsoft.Extensions.Logging;
using Stride.Core.Mathematics;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using VL.Core;
using VL.Core.CompilerServices;
using VL.Core.Reactive;
using TypeDescriptor = System.ComponentModel.TypeDescriptor;

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
            TypeDescriptor.AddAttributes(typeof(Int2), new TypeConverterAttribute(typeof(Int2Converter)));
            TypeDescriptor.AddAttributes(typeof(Int3), new TypeConverterAttribute(typeof(Int3Converter)));
            TypeDescriptor.AddAttributes(typeof(Int4), new TypeConverterAttribute(typeof(Int4Converter)));
        }

        public override void Configure(AppHost appHost)
        {
            Mathematics.Serialization.RegisterSerializers(appHost.Factory);
            appHost.Factory.RegisterSerializer<PublicChannelDescription, PublicChannelDescriptionSerializer>();

            appHost.Services.RegisterService<IChannelHub>(_ =>
            {
                var channelHub = new ChannelHub(appHost);
                // make sure all channels of config exist in app-channelhub.
                var watcher = ChannelHubConfigWatcher.FromApplicationBasePath(appHost);
                channelHub.MustHaveDescriptive = watcher.Descriptions;
                if (!appHost.IsExported && !appHost.IsClient)
                {
                    channelHub.OnChannelsChanged
                    .Throttle(TimeSpan.FromMilliseconds(500))
                    .Subscribe(_ =>
                    {
                        try
                        {
                            watcher.Save(channelHub.Channels
                                .Where(c => !c.Value.IsAnonymous())
                                .OrderBy(_ => _.Key)
                                .Select(_ =>
                                new PublicChannelDescription(_.Key, appHost.TypeRegistry.GetTypeInfo(_.Value.ClrTypeOfValues).FullName)).ToArray());
                        }
                        catch (Exception)
                        {
                            appHost.DefaultLogger.LogWarning($"writing {ChannelHubConfigWatcher.GetConfigFilePath(appHost)} failed.");
                        }
                    });
                }
                return channelHub;
            });

            // registering node factory producing nodes for global channels is necessary in any case.
            // compiler needs it to output correct code. target code calls into the nodes. So they need to be present as well.
            ChannelHubNodeBuilding.RegisterChannelHubNodeFactoryTriggeredViaConfigFile(appHost);
        }

        class SingleConverter : System.ComponentModel.SingleConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                if (sourceType == typeof(string) || sourceType == typeof(Vector2) || sourceType == typeof(Vector3) || sourceType == typeof(Vector4))
                    return true;
                return base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is string str)
                {
                    if (float.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
                        return result;
                }
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
                if (destinationType == typeof(string) || destinationType == typeof(Vector2) || destinationType == typeof(Vector3) || destinationType == typeof(Vector4))
                    return true;
                return base.CanConvertTo(context, destinationType);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                var singleValue = (float)value;
                if (destinationType == typeof(string))
                    return singleValue.ToString(CultureInfo.InvariantCulture);
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
                if (sourceType == typeof(string) || sourceType == typeof(Vector2) || sourceType == typeof(Vector3) || sourceType == typeof(Vector4))
                    return true;
                return base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is string str)
                {
                    if (double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
                        return result;
                }
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
                if (destinationType == typeof(string) || destinationType == typeof(Vector2) || destinationType == typeof(Vector3) || destinationType == typeof(Vector4))
                    return true;
                return base.CanConvertTo(context, destinationType);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                var doubleValue = (double)value;
                if (destinationType == typeof(string))
                    return doubleValue.ToString(CultureInfo.InvariantCulture);
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
                if (sourceType == typeof(string) || sourceType == typeof(float) || sourceType == typeof(Vector3) || sourceType == typeof(Vector4))
                    return true;
                return base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is string str)
                {
                    var parts = str.Split(',');
                    if (parts.Length == 2 &&
                        float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
                        float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
                    {
                        return new Vector2(x, y);
                    }
                }
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
                if (destinationType == typeof(string) || destinationType == typeof(float) || destinationType == typeof(Vector3) || destinationType == typeof(Vector4))
                    return true;
                return base.CanConvertTo(context, destinationType);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                var vector = (Vector2)value;
                if (destinationType == typeof(string))
                    return $"{vector.X.ToString(CultureInfo.InvariantCulture)}, {vector.Y.ToString(CultureInfo.InvariantCulture)}";
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
                if (sourceType == typeof(string) || sourceType == typeof(float) || sourceType == typeof(Vector2) || sourceType == typeof(Vector4))
                    return true;
                return base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is string str)
                {
                    var parts = str.Split(',');
                    if (parts.Length == 3 &&
                        float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
                        float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var y) &&
                        float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
                    {
                        return new Vector3(x, y, z);
                    }
                }
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
                if (destinationType == typeof(string) || destinationType == typeof(float) || destinationType == typeof(Vector2) || destinationType == typeof(Vector4))
                    return true;
                return base.CanConvertTo(context, destinationType);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                var vector = (Vector3)value;
                if (destinationType == typeof(string))
                    return $"{vector.X.ToString(CultureInfo.InvariantCulture)}, {vector.Y.ToString(CultureInfo.InvariantCulture)}, {vector.Z.ToString(CultureInfo.InvariantCulture)}";
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
                if (sourceType == typeof(string) || sourceType == typeof(float) || sourceType == typeof(Vector2) || sourceType == typeof(Vector3))
                    return true;
                return base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is string str)
                {
                    var parts = str.Split(',');
                    if (parts.Length == 4 &&
                        float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
                        float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var y) &&
                        float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var z) &&
                        float.TryParse(parts[3].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var w))
                    {
                        return new Vector4(x, y, z, w);
                    }
                }
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
                if (destinationType == typeof(string) || destinationType == typeof(float) || destinationType == typeof(Vector2) || destinationType == typeof(Vector3))
                    return true;
                return base.CanConvertTo(context, destinationType);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                var vector = (Vector4)value;
                if (destinationType == typeof(string))
                    return $"{vector.X.ToString(CultureInfo.InvariantCulture)}, {vector.Y.ToString(CultureInfo.InvariantCulture)}, {vector.Z.ToString(CultureInfo.InvariantCulture)}, {vector.W.ToString(CultureInfo.InvariantCulture)}";
                if (destinationType == typeof(float))
                    return vector.X;
                if (destinationType == typeof(Vector2))
                    return new Vector2(vector.X, vector.Y);
                if (destinationType == typeof(Vector3))
                    return new Vector3(vector.X, vector.Y, vector.Z);
                return base.ConvertTo(context, culture, value, destinationType);
            }
        }

        class Int2Converter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                if (sourceType == typeof(string) || sourceType == typeof(int) || sourceType == typeof(Int3) || sourceType == typeof(Int4))
                    return true;
                return base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is string str)
                {
                    var parts = str.Split(',');
                    if (parts.Length == 2 &&
                        int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var x) &&
                        int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var y))
                    {
                        return new Int2(x, y);
                    }
                }
                if (value is int)
                {
                    var intValue = (int)value;
                    return new Int2(intValue);
                }
                if (value is Int3)
                {
                    var vector = (Int3)value;
                    return new Int2(vector.X, vector.Y);
                }
                if (value is Int4)
                {
                    var vector = (Int4)value;
                    return new Int2(vector.X, vector.Y);
                }
                return base.ConvertFrom(context, culture, value);
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                if (destinationType == typeof(string) || destinationType == typeof(int) || destinationType == typeof(Int3) || destinationType == typeof(Int4))
                    return true;
                return base.CanConvertTo(context, destinationType);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                var vector = (Int2)value;
                if (destinationType == typeof(string))
                    return $"{vector.X.ToString(CultureInfo.InvariantCulture)}, {vector.Y.ToString(CultureInfo.InvariantCulture)}";
                if (destinationType == typeof(int))
                    return vector.X;
                if (destinationType == typeof(Int3))
                    return new Int3(vector.X, vector.Y, 0);
                if (destinationType == typeof(Int4))
                    return new Int4(vector.X, vector.Y, 0, 0);
                return base.ConvertTo(context, culture, value, destinationType);
            }
        }

        class Int3Converter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                if (sourceType == typeof(string) || sourceType == typeof(int) || sourceType == typeof(Int2) || sourceType == typeof(Int4))
                    return true;
                return base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is string str)
                {
                    var parts = str.Split(',');
                    if (parts.Length == 3 &&
                     int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var x) &&
                           int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var y) &&
               int.TryParse(parts[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var z))
                    {
                        return new Int3(x, y, z);
                    }
                }
                if (value is int)
                {
                    var intValue = (int)value;
                    return new Int3(intValue);
                }
                if (value is Int2)
                {
                    var vector = (Int2)value;
                    return new Int3(vector.X, vector.Y, 0);
                }
                if (value is Int4)
                {
                    var vector = (Int4)value;
                    return new Int3(vector.X, vector.Y, vector.Z);
                }
                return base.ConvertFrom(context, culture, value);
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                if (destinationType == typeof(string) || destinationType == typeof(int) || destinationType == typeof(Int2) || destinationType == typeof(Int4))
                    return true;
                return base.CanConvertTo(context, destinationType);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                var vector = (Int3)value;
                if (destinationType == typeof(string))
                    return $"{vector.X.ToString(CultureInfo.InvariantCulture)}, {vector.Y.ToString(CultureInfo.InvariantCulture)}, {vector.Z.ToString(CultureInfo.InvariantCulture)}";
                if (destinationType == typeof(int))
                    return vector.X;
                if (destinationType == typeof(Int2))
                    return new Int2(vector.X, vector.Y);
                if (destinationType == typeof(Int4))
                    return new Int4(vector.X, vector.Y, vector.Z, 0);
                return base.ConvertTo(context, culture, value, destinationType);
            }
        }

        class Int4Converter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                if (sourceType == typeof(string) || sourceType == typeof(int) || sourceType == typeof(Int2) || sourceType == typeof(Int3))
                    return true;
                return base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is string str)
                {
                    var parts = str.Split(',');
                    if (parts.Length == 4 &&
                        int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var x) &&
                        int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var y) &&
                        int.TryParse(parts[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var z) &&
                        int.TryParse(parts[3].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var w))
                    {
                        return new Int4(x, y, z, w);
                    }
                }
                if (value is int)
                {
                    var intValue = (int)value;
                    return new Int4(intValue);
                }
                if (value is Int2)
                {
                    var vector = (Int2)value;
                    return new Int4(vector.X, vector.Y, 0, 0);
                }
                if (value is Int3)
                {
                    var vector = (Int3)value;
                    return new Int4(vector.X, vector.Y, vector.Z, 0);
                }
                return base.ConvertFrom(context, culture, value);
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                if (destinationType == typeof(string) || destinationType == typeof(int) || destinationType == typeof(Int2) || destinationType == typeof(Int3))
                    return true;
                return base.CanConvertTo(context, destinationType);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                var vector = (Int4)value;
                if (destinationType == typeof(string))
                    return $"{vector.X.ToString(CultureInfo.InvariantCulture)}, {vector.Y.ToString(CultureInfo.InvariantCulture)}, {vector.Z.ToString(CultureInfo.InvariantCulture)}, {vector.W.ToString(CultureInfo.InvariantCulture)}";
                if (destinationType == typeof(int))
                    return vector.X;
                if (destinationType == typeof(Int2))
                    return new Int2(vector.X, vector.Y);
                if (destinationType == typeof(Int3))
                    return new Int3(vector.X, vector.Y, vector.Z);
                return base.ConvertTo(context, culture, value, destinationType);
            }
        }
    }
}
