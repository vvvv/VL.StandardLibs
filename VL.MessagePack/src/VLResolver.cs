using MessagePack.Formatters;
using MessagePack;
using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Half = Stride.Core.Mathematics.Half;

namespace VL.MessagePack
{
    public class VLResolver : IFormatterResolver
    {
        public static readonly VLResolver Instance = new VLResolver();

        private VLResolver()
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
                Formatter = (IMessagePackFormatter<T>?)VLResolveryResolverGetFormatterHelper.GetFormatter(typeof(T));
            }
        }
    }
    internal static class VLResolveryResolverGetFormatterHelper
    {
        private static readonly Dictionary<Type, object> FormatterMap = new Dictionary<Type, object>()
        {
            // standard
            { typeof(AngleSingle), new AngleSingleFormatter() },
            { typeof(BoundingBox), new BoundingBoxFormatter() },
            { typeof(BoundingBoxExt), new BoundingBoxExtFormatter() },
            //{ typeof(BoundingFrustum), new Vector4Formatter() },
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

            

            

            

           

            

            

            


            
            
            

            //// standard + array
            //{ typeof(Vector2[]), new ArrayFormatter<Vector2>() },
            //{ typeof(Vector3[]), new ArrayFormatter<Vector3>() },
            //{ typeof(Vector4[]), new ArrayFormatter<Vector4>() },
            //{ typeof(Quaternion[]), new ArrayFormatter<Quaternion>() },
            //{ typeof(Color[]), new ArrayFormatter<Color>() },
            //{ typeof(Bounds[]), new ArrayFormatter<Bounds>() },
            //{ typeof(Rect[]), new ArrayFormatter<Rect>() },
            //{ typeof(Vector2?[]), new ArrayFormatter<Vector2?>() },
            //{ typeof(Vector3?[]), new ArrayFormatter<Vector3?>() },
            //{ typeof(Vector4?[]), new ArrayFormatter<Vector4?>() },
            //{ typeof(Quaternion?[]), new ArrayFormatter<Quaternion?>() },
            //{ typeof(Color?[]), new ArrayFormatter<Color?>() },
            //{ typeof(Bounds?[]), new ArrayFormatter<Bounds?>() },
            //{ typeof(Rect?[]), new ArrayFormatter<Rect?>() },

            //// standard + list
            //{ typeof(List<Vector2>), new ListFormatter<Vector2>() },
            //{ typeof(List<Vector3>), new ListFormatter<Vector3>() },
            //{ typeof(List<Vector4>), new ListFormatter<Vector4>() },
            //{ typeof(List<Quaternion>), new ListFormatter<Quaternion>() },
            //{ typeof(List<Color>), new ListFormatter<Color>() },
            //{ typeof(List<Bounds>), new ListFormatter<Bounds>() },
            //{ typeof(List<Rect>), new ListFormatter<Rect>() },
            //{ typeof(List<Vector2?>), new ListFormatter<Vector2?>() },
            //{ typeof(List<Vector3?>), new ListFormatter<Vector3?>() },
            //{ typeof(List<Vector4?>), new ListFormatter<Vector4?>() },
            //{ typeof(List<Quaternion?>), new ListFormatter<Quaternion?>() },
            //{ typeof(List<Color?>), new ListFormatter<Color?>() },
            //{ typeof(List<Bounds?>), new ListFormatter<Bounds?>() },
            //{ typeof(List<Rect?>), new ListFormatter<Rect?>() },

            //// new
            //{ typeof(AnimationCurve),     new AnimationCurveFormatter() },
            //{ typeof(RectOffset),         new RectOffsetFormatter() },
            //{ typeof(Gradient),           new GradientFormatter() },
            //{ typeof(WrapMode),           new WrapModeFormatter() },
            //{ typeof(GradientMode),       new GradientModeFormatter() },
            //{ typeof(Keyframe),           new KeyframeFormatter() },
            //{ typeof(Matrix4x4),          new Matrix4x4Formatter() },
            //{ typeof(GradientColorKey),   new GradientColorKeyFormatter() },
            //{ typeof(GradientAlphaKey),   new GradientAlphaKeyFormatter() },
            //{ typeof(Color32),            new Color32Formatter() },
            //{ typeof(LayerMask),          new LayerMaskFormatter() },
            //{ typeof(WrapMode?),          new StaticNullableFormatter<WrapMode>(new WrapModeFormatter()) },
            //{ typeof(GradientMode?),      new StaticNullableFormatter<GradientMode>(new GradientModeFormatter()) },
            //{ typeof(Keyframe?),          new StaticNullableFormatter<Keyframe>(new KeyframeFormatter()) },
            //{ typeof(Matrix4x4?),         new StaticNullableFormatter<Matrix4x4>(new Matrix4x4Formatter()) },
            //{ typeof(GradientColorKey?),  new StaticNullableFormatter<GradientColorKey>(new GradientColorKeyFormatter()) },
            //{ typeof(GradientAlphaKey?),  new StaticNullableFormatter<GradientAlphaKey>(new GradientAlphaKeyFormatter()) },
            //{ typeof(Color32?),           new StaticNullableFormatter<Color32>(new Color32Formatter()) },
            //{ typeof(LayerMask?),         new StaticNullableFormatter<LayerMask>(new LayerMaskFormatter()) },

            //// new + array
            //{ typeof(AnimationCurve[]),     new ArrayFormatter<AnimationCurve>() },
            //{ typeof(RectOffset[]),         new ArrayFormatter<RectOffset>() },
            //{ typeof(Gradient[]),           new ArrayFormatter<Gradient>() },
            //{ typeof(WrapMode[]),           new ArrayFormatter<WrapMode>() },
            //{ typeof(GradientMode[]),       new ArrayFormatter<GradientMode>() },
            //{ typeof(Keyframe[]),           new ArrayFormatter<Keyframe>() },
            //{ typeof(Matrix4x4[]),          new ArrayFormatter<Matrix4x4>() },
            //{ typeof(GradientColorKey[]),   new ArrayFormatter<GradientColorKey>() },
            //{ typeof(GradientAlphaKey[]),   new ArrayFormatter<GradientAlphaKey>() },
            //{ typeof(Color32[]),            new ArrayFormatter<Color32>() },
            //{ typeof(LayerMask[]),          new ArrayFormatter<LayerMask>() },
            //{ typeof(WrapMode?[]),          new ArrayFormatter<WrapMode?>() },
            //{ typeof(GradientMode?[]),      new ArrayFormatter<GradientMode?>() },
            //{ typeof(Keyframe?[]),          new ArrayFormatter<Keyframe?>() },
            //{ typeof(Matrix4x4?[]),         new ArrayFormatter<Matrix4x4?>() },
            //{ typeof(GradientColorKey?[]),  new ArrayFormatter<GradientColorKey?>() },
            //{ typeof(GradientAlphaKey?[]),  new ArrayFormatter<GradientAlphaKey?>() },
            //{ typeof(Color32?[]),           new ArrayFormatter<Color32?>() },
            //{ typeof(LayerMask?[]),         new ArrayFormatter<LayerMask?>() },

            //// new + list
            //{ typeof(List<AnimationCurve>),     new ListFormatter<AnimationCurve>() },
            //{ typeof(List<RectOffset>),         new ListFormatter<RectOffset>() },
            //{ typeof(List<Gradient>),           new ListFormatter<Gradient>() },
            //{ typeof(List<WrapMode>),           new ListFormatter<WrapMode>() },
            //{ typeof(List<GradientMode>),       new ListFormatter<GradientMode>() },
            //{ typeof(List<Keyframe>),           new ListFormatter<Keyframe>() },
            //{ typeof(List<Matrix4x4>),          new ListFormatter<Matrix4x4>() },
            //{ typeof(List<GradientColorKey>),   new ListFormatter<GradientColorKey>() },
            //{ typeof(List<GradientAlphaKey>),   new ListFormatter<GradientAlphaKey>() },
            //{ typeof(List<Color32>),            new ListFormatter<Color32>() },
            //{ typeof(List<LayerMask>),          new ListFormatter<LayerMask>() },
            //{ typeof(List<WrapMode?>),          new ListFormatter<WrapMode?>() },
            //{ typeof(List<GradientMode?>),      new ListFormatter<GradientMode?>() },
            //{ typeof(List<Keyframe?>),          new ListFormatter<Keyframe?>() },
            //{ typeof(List<Matrix4x4?>),         new ListFormatter<Matrix4x4?>() },
            //{ typeof(List<GradientColorKey?>),  new ListFormatter<GradientColorKey?>() },
            //{ typeof(List<GradientAlphaKey?>),  new ListFormatter<GradientAlphaKey?>() },
            //{ typeof(List<Color32?>),           new ListFormatter<Color32?>() },
            //{ typeof(List<LayerMask?>),         new ListFormatter<LayerMask?>() },
        };

        internal static object? GetFormatter(Type t)
        {
            object formatter;
            if (FormatterMap.TryGetValue(t, out formatter))
            {
                return formatter;
            }

            return null;
        }
    }
}
