using MessagePack.Formatters;
using MessagePack;
using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using VL.Serialization.MessagePack.Formatters;
using Half = Stride.Core.Mathematics.Half;
using HalfFormatter = VL.Serialization.MessagePack.Formatters.HalfFormatter;


namespace VL.Serialization.MessagePack
{
    sealed class StrideResolver : IFormatterResolver
    {
        public static readonly StrideResolver Instance = new StrideResolver();

        private StrideResolver()
        {
        }

        public IMessagePackFormatter<T>? GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        private static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T>? Formatter;

            static FormatterCache()
            {
                Formatter = (IMessagePackFormatter<T>?)StrideResolverGetFormatterHelper.GetFormatter(typeof(T));
            }
        }
    }
    internal static class StrideResolverGetFormatterHelper
    {
        private static readonly Dictionary<Type, object> FormatterMap = new Dictionary<Type, object>()
        {
            // standard
            { typeof(AngleSingle), new AngleSingleFormatter() },
            { typeof(BoundingBox), new BoundingBoxFormatter() },
            { typeof(BoundingBoxExt), new BoundingBoxExtFormatter() },
            //{ typeof(BoundingFrustum), new BoundingFrustumFormatter() },
            { typeof(BoundingSphere), new BoundingSphereFormatter() },
            { typeof(Color), new ColorFormatter() },
            { typeof(Color3), new Color3Formatter() },
            { typeof(Color4), new Color4Formatter() },
            { typeof(ColorBGRA), new ColorBGRAFormatter() },
            { typeof(ColorHSV), new ColorHSVFormatter() },
            { typeof(Double2), new Double2Formatter() },
            { typeof(Double3), new Double3Formatter() },
            { typeof(Double4), new Double4Formatter() },
            { typeof(Half),  new HalfFormatter() },
            { typeof(Half2), new Half2Formatter() },
            { typeof(Half3), new Half3Formatter() },
            { typeof(Half4), new Half4Formatter() },
            { typeof(Int2), new Int2Formatter() },
            { typeof(Int3), new Int3Formatter() },
            { typeof(Int4), new Int4Formatter() },
            { typeof(Matrix), new MatrixFormatter() },
            { typeof(Plane), new PlaneFormatter() },
            { typeof(Point), new PointFormatter() },
            { typeof(Quaternion), new QuaternionFormatter() },
            { typeof(Ray), new RayFormatter() },
            { typeof(Rectangle), new RectangleFormatter() },
            { typeof(RectangleF), new RectangleFFormatter() },
            { typeof(Size2), new Size2Formatter() },
            { typeof(Size2F), new Size2FFormatter() },
            { typeof(Size3), new Size3Formatter() },
            { typeof(UInt4), new UInt4Formatter() },
            { typeof(Vector2), new Vector2Formatter() },
            { typeof(Vector3), new Vector3Formatter() },
            { typeof(Vector4), new Vector4Formatter() },
        };

        internal static object? GetFormatter(Type t)
        {
            object? formatter;
            if (FormatterMap.TryGetValue(t, out formatter))
            {
                return formatter;
            }

            return null;
        }
    }
}
