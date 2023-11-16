using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance;
using MessagePack;
using MessagePack.Formatters;
using Stride.Core.Mathematics;
using Half = Stride.Core.Mathematics.Half;

namespace VL.Serialization.MessagePack.Formatters
{
    #region Angle
    sealed class AngleSingleFormatter : IMessagePackFormatter<AngleSingle>
    {
        public void Serialize(ref MessagePackWriter writer, AngleSingle value, MessagePackSerializerOptions options)
        {
            writer.Write(value.Radians);
        }

        public AngleSingle Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            return new AngleSingle(reader.ReadSingle(), AngleType.Radian);
        }
    }
    #endregion

    #region Bounding
    sealed class BoundingBoxFormatter : IMessagePackFormatter<BoundingBox>
    {
        IMessagePackFormatter<Vector3>? formatter = null;

        public void Serialize(ref MessagePackWriter writer, BoundingBox value, MessagePackSerializerOptions options)
        {
            if (formatter == null) 
            {
                formatter = options.Resolver.GetFormatterWithVerify<Vector3>();
            }

            writer.WriteArrayHeader(2);
            formatter.Serialize(ref writer, value.Minimum, options);
            formatter.Serialize(ref writer, value.Maximum, options);
        }

        public BoundingBox Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            if (formatter == null)
            {
                formatter = options.Resolver.GetFormatterWithVerify<Vector3>();
            }

            var length = reader.ReadArrayHeader();
            var min = default(Vector3);
            var max = default(Vector3);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        min = formatter.Deserialize(ref reader, options);
                        break;
                    case 1:
                        max = formatter.Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new BoundingBox(min, max);
            return result;
        }
    }
    
    sealed class BoundingBoxExtFormatter : IMessagePackFormatter<BoundingBoxExt>
    {
        IMessagePackFormatter<Vector3>? formatter = null;

        public void Serialize(ref MessagePackWriter writer, BoundingBoxExt value, MessagePackSerializerOptions options)
        {
            if (formatter == null)
            {
                formatter = options.Resolver.GetFormatterWithVerify<Vector3>();
            }

            writer.WriteArrayHeader(2);
            formatter.Serialize(ref writer, value.Minimum, options);
            formatter.Serialize(ref writer, value.Maximum, options);
        }

        public BoundingBoxExt Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            if (formatter == null)
            {
                formatter = options.Resolver.GetFormatterWithVerify<Vector3>();
            }

            var length = reader.ReadArrayHeader();
            var min = default(Vector3);
            var max = default(Vector3);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        min = formatter.Deserialize(ref reader, options);
                        break;
                    case 1:
                        max = formatter.Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new BoundingBoxExt(min, max);
            return result;
        }
    }

    //sealed class BoundingFrustumFormatter : IMessagePackFormatter<BoundingFrustum>
    //{
    //    IMessagePackFormatter<Plane>? formatter = null;

    //    public void Serialize(ref MessagePackWriter writer, BoundingFrustum value, MessagePackSerializerOptions options)
    //    {
    //        if (formatter == null)
    //        {
    //            formatter = options.Resolver.GetFormatterWithVerify<Plane>();
    //        }

    //        writer.WriteArrayHeader(2);
    //        formatter.Serialize(ref writer, value.LeftPlane, options);
    //        formatter.Serialize(ref writer, value.RightPlane, options);
    //        formatter.Serialize(ref writer, value.TopPlane, options);
    //        formatter.Serialize(ref writer, value.BottomPlane, options);
    //        formatter.Serialize(ref writer, value.NearPlane, options);
    //        formatter.Serialize(ref writer, value.FarPlane, options);
    //    }

    //    public BoundingFrustum Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    //    {
    //        if (reader.IsNil)
    //        {
    //            throw new InvalidOperationException("typecode is null, struct not supported");
    //        }

    //        if (formatter == null)
    //        {
    //            formatter = options.Resolver.GetFormatterWithVerify<Plane>();
    //        }

    //        var length = reader.ReadArrayHeader();
    //        var min = default(Vector3);
    //        var max = default(Vector3);
    //        for (int i = 0; i < length; i++)
    //        {
    //            var key = i;
    //            switch (key)
    //            {
    //                case 0:
    //                    min = formatter.Deserialize(ref reader, options);
    //                    break;
    //                case 1:
    //                    max = formatter.Deserialize(ref reader, options);
    //                    break;
    //                default:
    //                    reader.Skip();
    //                    break;
    //            }
    //        }

    //        var result = new BoundingBox(min, max);
    //        return result;
    //    }
    //}

    sealed class BoundingSphereFormatter : IMessagePackFormatter<BoundingSphere>
    {
        IMessagePackFormatter<Vector3>? formatter = null;

        public void Serialize(ref MessagePackWriter writer, BoundingSphere value, MessagePackSerializerOptions options)
        {
            if (formatter == null)
            {
                formatter = options.Resolver.GetFormatterWithVerify<Vector3>();
            }

            writer.WriteArrayHeader(2);
            formatter.Serialize(ref writer, value.Center, options);
            writer.Write(value.Radius);
        }

        public BoundingSphere Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            if (formatter == null)
            {
                formatter = options.Resolver.GetFormatterWithVerify<Vector3>();
            }

            var length = reader.ReadArrayHeader();
            var center = default(Vector3);
            var radius = default(float);
            
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        center = formatter.Deserialize(ref reader, options);
                        break;
                    case 1:
                        radius = reader.ReadSingle();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new BoundingSphere(center,radius);
            return result;
        }
    }
    #endregion Bounding

    #region Color
    sealed class ColorFormatter : IMessagePackFormatter<Color>
    {
        public void Serialize(ref MessagePackWriter writer, Color value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            writer.Write(value.R);
            writer.Write(value.G);
            writer.Write(value.B);
            writer.Write(value.A);
        }

        public Color Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var r = default(byte);
            var g = default(byte);
            var b = default(byte);
            var a = default(byte);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        r = reader.ReadByte();
                        break;
                    case 1:
                        g = reader.ReadByte();
                        break;
                    case 2:
                        b = reader.ReadByte();
                        break;
                    case 3:
                        a = reader.ReadByte();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new Color(r, g, b, a);
            return result;
        }
    }

    sealed class Color3Formatter : IMessagePackFormatter<Color3>
    {
        public void Serialize(ref MessagePackWriter writer, Color3 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            writer.Write(value.R);
            writer.Write(value.G);
            writer.Write(value.B);
        }

        public Color3 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var r = default(float);
            var g = default(float);
            var b = default(float);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        r = reader.ReadSingle();
                        break;
                    case 1:
                        g = reader.ReadSingle();
                        break;
                    case 2:
                        b = reader.ReadSingle();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new Color3(r, g, b);
            return result;
        }
    }

    sealed class Color4Formatter : IMessagePackFormatter<Color4>
    {
        public void Serialize(ref MessagePackWriter writer, Color4 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            writer.Write(value.R);
            writer.Write(value.G);
            writer.Write(value.B);
            writer.Write(value.A);
        }

        public Color4 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var r = default(float);
            var g = default(float);
            var b = default(float);
            var a = default(float);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        r = reader.ReadSingle();
                        break;
                    case 1:
                        g = reader.ReadSingle();
                        break;
                    case 2:
                        b = reader.ReadSingle();
                        break;
                    case 3:
                        a = reader.ReadSingle();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new Color4(r, g, b, a);
            return result;
        }
    }

    sealed class ColorBGRAFormatter : IMessagePackFormatter<ColorBGRA>
    {
        public void Serialize(ref MessagePackWriter writer, ColorBGRA value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            writer.Write(value.B);
            writer.Write(value.G);
            writer.Write(value.R);
            writer.Write(value.A);
        }

        public ColorBGRA Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var b = default(byte);
            var g = default(byte);
            var r = default(byte);
            var a = default(byte);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        b = reader.ReadByte();
                        break;
                    case 1:
                        g = reader.ReadByte();
                        break;
                    case 2:
                        r = reader.ReadByte();
                        break;
                    case 3:
                        a = reader.ReadByte();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new ColorBGRA(r, g, b, a);
            return result;
        }
    }

    sealed class ColorHSVFormatter : IMessagePackFormatter<ColorHSV>
    {
        public void Serialize(ref MessagePackWriter writer, ColorHSV value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            writer.Write(value.H);
            writer.Write(value.S);
            writer.Write(value.V);
            writer.Write(value.A);
        }

        public ColorHSV Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var h = default(float);
            var s = default(float);
            var v = default(float);
            var a = default(float);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        h = reader.ReadSingle();
                        break;
                    case 1:
                        s = reader.ReadSingle();
                        break;
                    case 2:
                        v = reader.ReadSingle();
                        break;
                    case 3:
                        a = reader.ReadSingle();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new ColorHSV(h, s, v, a);
            return result;
        }
    }
    #endregion Color

    #region Double
    sealed class Double2Formatter : IMessagePackFormatter<Double2>
    {
        public void Serialize(ref MessagePackWriter writer, Double2 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            writer.Write(value.X);
            writer.Write(value.Y);
        }

        public Double2 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var x = default(double);
            var y = default(double);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        x = reader.ReadDouble();
                        break;
                    case 1:
                        y = reader.ReadDouble();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new Double2(x, y);
            return result;
        }
    }
    
    sealed class Double3Formatter : IMessagePackFormatter<Double3>
    {
        public void Serialize(ref MessagePackWriter writer, Double3 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            writer.Write(value.X);
            writer.Write(value.Y);
            writer.Write(value.Z);
        }

        public Double3 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var x = default(double);
            var y = default(double);
            var z = default(double);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        x = reader.ReadDouble();
                        break;
                    case 1:
                        y = reader.ReadDouble();
                        break;
                    case 2:
                        z = reader.ReadDouble();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new Double3(x, y, z);
            return result;
        }
    }
    
    sealed class Double4Formatter : IMessagePackFormatter<Double4>
    {
        public void Serialize(ref MessagePackWriter writer, Double4 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            writer.Write(value.X);
            writer.Write(value.Y);
            writer.Write(value.Z);
            writer.Write(value.W);
        }

        public Double4 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var x = default(double);
            var y = default(double);
            var z = default(double);
            var w = default(double);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        x = reader.ReadDouble();
                        break;
                    case 1:
                        y = reader.ReadDouble();
                        break;
                    case 2:
                        z = reader.ReadDouble();
                        break;
                    case 3:
                        w = reader.ReadDouble();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new Double4(x, y, z, w);
            return result;
        }
    }
    #endregion Double

    #region Half
    sealed class HalfFormatter : IMessagePackFormatter<Half>
    {
        public void Serialize(ref MessagePackWriter writer, Half value, MessagePackSerializerOptions options)
        {
            writer.Write(value.RawValue);
        }

        public Half Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }
            return (Half)reader.ReadUInt16();
        }
    }

    sealed class Half2Formatter : IMessagePackFormatter<Half2>
    {
        public void Serialize(ref MessagePackWriter writer, Half2 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            writer.Write(value.X.RawValue);
            writer.Write(value.Y.RawValue);
        }

        public Half2 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var x = default(Half);
            var y = default(Half);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        x = (Half)reader.ReadUInt16();
                        break;
                    case 1:
                        y = (Half)reader.ReadUInt16();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new Half2(x, y);
            return result;
        }
    }

    sealed class Half3Formatter : IMessagePackFormatter<Half3>
    {
        public void Serialize(ref MessagePackWriter writer, Half3 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            writer.Write(value.X.RawValue);
            writer.Write(value.Y.RawValue);
            writer.Write(value.Z.RawValue);
        }

        public Half3 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var x = default(Half);
            var y = default(Half);
            var z = default(Half);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        x = (Half)reader.ReadUInt16();
                        break;
                    case 1:
                        y = (Half)reader.ReadUInt16();
                        break;
                    case 2:
                        z = (Half)reader.ReadUInt16();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new Half3(x, y, z);
            return result;
        }
    }

    sealed class Half4Formatter : IMessagePackFormatter<Half4>
    {
        public void Serialize(ref MessagePackWriter writer, Half4 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            writer.Write(value.X.RawValue);
            writer.Write(value.Y.RawValue);
            writer.Write(value.Z.RawValue);
            writer.Write(value.W.RawValue);
        }

        public Half4 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var x = default(Half);
            var y = default(Half);
            var z = default(Half);
            var w = default(Half);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        x = (Half)reader.ReadUInt16();
                        break;
                    case 1:
                        y = (Half)reader.ReadUInt16();
                        break;
                    case 2:
                        z = (Half)reader.ReadUInt16();
                        break;
                    case 3:
                        w = (Half)reader.ReadUInt16();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new Half4(x, y, z, w);
            return result;
        }
    }
    #endregion Half

    #region Int
    sealed class Int2Formatter : IMessagePackFormatter<Int2>
    {
        public void Serialize(ref MessagePackWriter writer, Int2 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            writer.Write(value.X);
            writer.Write(value.Y);
        }

        public Int2 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var x = default(int);
            var y = default(int);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        x = reader.ReadInt32();
                        break;
                    case 1:
                        y = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new Int2(x, y);
            return result;
        }
    }

    sealed class Int3Formatter : IMessagePackFormatter<Int3>
    {
        public void Serialize(ref MessagePackWriter writer, Int3 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            writer.Write(value.X);
            writer.Write(value.Y);
            writer.Write(value.Z);
        }

        public Int3 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var x = default(int);
            var y = default(int);
            var z = default(int);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        x = reader.ReadInt32();
                        break;
                    case 1:
                        y = reader.ReadInt32();
                        break;
                    case 2:
                        z = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new Int3(x, y, z);
            return result;
        }
    }

    sealed class Int4Formatter : IMessagePackFormatter<Int4>
    {
        public void Serialize(ref MessagePackWriter writer, Int4 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            writer.Write(value.X);
            writer.Write(value.Y);
            writer.Write(value.Z);
            writer.Write(value.W);
        }

        public Int4 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var x = default(int);
            var y = default(int);
            var z = default(int);
            var w = default(int);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        x = reader.ReadInt32();
                        break;
                    case 1:
                        y = reader.ReadInt32();
                        break;
                    case 2:
                        z = reader.ReadInt32();
                        break;
                    case 3:
                        w = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new Int4(x, y, z, w);
            return result;
        }
    }
    #endregion Int

    #region Geo
    sealed class MatrixFormatter : IMessagePackFormatter<Matrix>
    {
        public void Serialize(ref MessagePackWriter writer, Matrix value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(16);
            writer.Write(value.M11);
            writer.Write(value.M12);
            writer.Write(value.M13);
            writer.Write(value.M14);
            writer.Write(value.M21);
            writer.Write(value.M22);
            writer.Write(value.M23);
            writer.Write(value.M24);
            writer.Write(value.M31);
            writer.Write(value.M32);
            writer.Write(value.M33);
            writer.Write(value.M34);
            writer.Write(value.M41);
            writer.Write(value.M42);
            writer.Write(value.M43);
            writer.Write(value.M44);
        }

        public Matrix Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var m11 = default(float);
            var m12 = default(float);
            var m13 = default(float);
            var m14 = default(float);
            var m21 = default(float);
            var m22 = default(float);
            var m23 = default(float);
            var m24 = default(float);
            var m31 = default(float);
            var m32 = default(float);
            var m33 = default(float);
            var m34 = default(float);
            var m41 = default(float);
            var m42 = default(float);
            var m43 = default(float);
            var m44 = default(float);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        m11 = reader.ReadSingle();
                        break;
                    case 1:
                        m12 = reader.ReadSingle();
                        break;
                    case 2:
                        m13 = reader.ReadSingle();
                        break;
                    case 3:
                        m14 = reader.ReadSingle();
                        break;
                    case 4:
                        m21 = reader.ReadSingle();
                        break;
                    case 5:
                        m22 = reader.ReadSingle();
                        break;
                    case 6:
                        m23 = reader.ReadSingle();
                        break;
                    case 7:
                        m24 = reader.ReadSingle();
                        break;
                    case 8:
                        m31 = reader.ReadSingle();
                        break;
                    case 9:
                        m32 = reader.ReadSingle();
                        break;
                    case 10:
                        m33 = reader.ReadSingle();
                        break;
                    case 11:
                        m34 = reader.ReadSingle();
                        break;
                    case 12:
                        m41 = reader.ReadSingle();
                        break;
                    case 13:
                        m42 = reader.ReadSingle();
                        break;
                    case 14:
                        m43 = reader.ReadSingle();
                        break;
                    case 15:
                        m44 = reader.ReadSingle();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new Matrix(m11,m12,m13,m14,m21,m22,m23,m24,m31,m32,m33,m34,m41,m42,m43,m44);
            return result;
        }
    }

    sealed class PlaneFormatter : IMessagePackFormatter<Plane>
    {
        IMessagePackFormatter<Vector3>? formatter = null;

        public void Serialize(ref MessagePackWriter writer, Plane value, MessagePackSerializerOptions options)
        {
            if (formatter == null)
            {
                formatter = options.Resolver.GetFormatterWithVerify<Vector3>();
            }

            writer.WriteArrayHeader(2);
            formatter.Serialize(ref writer, value.Normal, options);
            writer.Write(value.D);
        }

        public Plane Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            if (formatter == null)
            {
                formatter = options.Resolver.GetFormatterWithVerify<Vector3>();
            }

            var length = reader.ReadArrayHeader();
            var normal = default(Vector3);
            var distance = default(float);

            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        normal = formatter.Deserialize(ref reader, options);
                        break;
                    case 1:
                        distance = reader.ReadSingle();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new Plane(normal, distance);
            return result;
        }
    }

    sealed class PointFormatter : IMessagePackFormatter<Point>
    {
        public void Serialize(ref MessagePackWriter writer, Point value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            writer.Write(value.X);
            writer.Write(value.Y);
        }

        public Point Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var x = default(int);
            var y = default(int);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        x = reader.ReadInt32();
                        break;
                    case 1:
                        y = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new Point(x, y);
            return result;
        }
    }

    sealed class QuaternionFormatter : IMessagePackFormatter<Quaternion>
    {
        public void Serialize(ref MessagePackWriter writer, Quaternion value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            writer.Write(value.X);
            writer.Write(value.Y);
            writer.Write(value.Z);
            writer.Write(value.W);
        }

        public Quaternion Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var x = default(float);
            var y = default(float);
            var z = default(float);
            var w = default(float);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        x = reader.ReadSingle();
                        break;
                    case 1:
                        y = reader.ReadSingle();
                        break;
                    case 2:
                        z = reader.ReadSingle();
                        break;
                    case 3:
                        w = reader.ReadSingle();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new Quaternion(x, y, z, w);
            return result;
        }
    }

    sealed class RayFormatter : IMessagePackFormatter<Ray>
    {
        IMessagePackFormatter<Vector3>? formatter = null;

        public void Serialize(ref MessagePackWriter writer, Ray value, MessagePackSerializerOptions options)
        {
            if (formatter == null)
            {
                formatter = options.Resolver.GetFormatterWithVerify<Vector3>();
            }

            writer.WriteArrayHeader(2);
            formatter.Serialize(ref writer, value.Position, options);
            formatter.Serialize(ref writer, value.Direction, options);
        }

        public Ray Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            if (formatter == null)
            {
                formatter = options.Resolver.GetFormatterWithVerify<Vector3>();
            }

            var length = reader.ReadArrayHeader();
            var position = default(Vector3);
            var direction = default(Vector3);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        position = formatter.Deserialize(ref reader, options);
                        break;
                    case 1:
                        direction = formatter.Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new Ray(position, direction);
            return result;
        }
    }
    #endregion

    #region Rect
    sealed class RectangleFormatter : IMessagePackFormatter<Rectangle>
    {
        public void Serialize(ref MessagePackWriter writer, Rectangle value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            writer.Write(value.X);
            writer.Write(value.Y);
            writer.Write(value.Width);
            writer.Write(value.Height);
        }

        public Rectangle Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var x = default(int);
            var y = default(int);
            var width = default(int);
            var height = default(int);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        x = reader.ReadInt32();
                        break;
                    case 1:
                        y = reader.ReadInt32();
                        break;
                    case 2:
                        width = reader.ReadInt32();
                        break;
                    case 3:
                        height = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new Rectangle(x, y, width, height);
            return result;
        }
    }

    sealed class RectangleFFormatter : IMessagePackFormatter<RectangleF>
    {
        public void Serialize(ref MessagePackWriter writer, RectangleF value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            writer.Write(value.X);
            writer.Write(value.Y);
            writer.Write(value.Width);
            writer.Write(value.Height);
        }

        public RectangleF Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var x = default(float);
            var y = default(float);
            var width = default(float);
            var height = default(float);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        x = reader.ReadSingle();
                        break;
                    case 1:
                        y = reader.ReadSingle();
                        break;
                    case 2:
                        width = reader.ReadSingle();
                        break;
                    case 3:
                        height = reader.ReadSingle();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new RectangleF(x, y, width, height);
            return result;
        }
    }
    #endregion

    #region Size
    sealed class Size2Formatter : IMessagePackFormatter<Size2>
    {
        public void Serialize(ref MessagePackWriter writer, Size2 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            writer.Write(value.Width);
            writer.Write(value.Height);
        }

        public Size2 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var width = default(int);
            var height = default(int);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        width = reader.ReadInt32();
                        break;
                    case 1:
                        height = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new Size2(width, height);
            return result;
        }
    }

    sealed class Size2FFormatter : IMessagePackFormatter<Size2F>
    {
        public void Serialize(ref MessagePackWriter writer, Size2F value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            writer.Write(value.Width);
            writer.Write(value.Height);
        }

        public Size2F Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var width = default(float);
            var height = default(float);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        width = reader.ReadSingle();
                        break;
                    case 1:
                        height = reader.ReadSingle();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new Size2F(width, height);
            return result;
        }
    }

    sealed class Size3Formatter : IMessagePackFormatter<Size3>
    {
        public void Serialize(ref MessagePackWriter writer, Size3 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            writer.Write(value.Width);
            writer.Write(value.Height);
            writer.Write(value.Depth);
        }

        public Size3 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var width = default(int);
            var height = default(int);
            var depth = default(int);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        width = reader.ReadInt32();
                        break;
                    case 1:
                        height = reader.ReadInt32();
                        break;
                    case 2:
                        depth = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new Size3(width, height, depth);
            return result;
        }
    }

    sealed class UInt4Formatter : IMessagePackFormatter<UInt4>
    {
        public void Serialize(ref MessagePackWriter writer, UInt4 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            writer.Write(value.X);
            writer.Write(value.Y);
            writer.Write(value.Z);
            writer.Write(value.W);
        }

        public UInt4 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var x = default(uint);
            var y = default(uint);
            var z = default(uint);
            var w = default(uint);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        x = reader.ReadUInt32();
                        break;
                    case 1:
                        y = reader.ReadUInt32();
                        break;
                    case 2:
                        z = reader.ReadUInt32();
                        break;
                    case 3:
                        w = reader.ReadUInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new UInt4(x, y, z, w);
            return result;
        }
    }
    #endregion

    #region Vector
    sealed class Vector2Formatter : IMessagePackFormatter<Vector2>
    {
        public void Serialize(ref MessagePackWriter writer, Vector2 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            writer.Write(value.X);
            writer.Write(value.Y);
        }

        public Vector2 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var x = default(float);
            var y = default(float);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        x = reader.ReadSingle();
                        break;
                    case 1:
                        y = reader.ReadSingle();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new Vector2(x, y);
            return result;
        }
    }

    sealed class Vector3Formatter : IMessagePackFormatter<Vector3>
    {
        public void Serialize(ref MessagePackWriter writer, Vector3 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            writer.Write(value.X);
            writer.Write(value.Y);
            writer.Write(value.Z);
        }

        public Vector3 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var x = default(float);
            var y = default(float);
            var z = default(float);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        x = reader.ReadSingle();
                        break;
                    case 1:
                        y = reader.ReadSingle();
                        break;
                    case 2:
                        z = reader.ReadSingle();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new Vector3(x, y, z);
            return result;
        }
    }

    sealed class Vector4Formatter : IMessagePackFormatter<Vector4>
    {
        public void Serialize(ref MessagePackWriter writer, Vector4 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);
            writer.Write(value.X);
            writer.Write(value.Y);
            writer.Write(value.Z);
            writer.Write(value.W);
        }

        public Vector4 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var x = default(float);
            var y = default(float);
            var z = default(float);
            var w = default(float);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        x = reader.ReadSingle();
                        break;
                    case 1:
                        y = reader.ReadSingle();
                        break;
                    case 2:
                        z = reader.ReadSingle();
                        break;
                    case 3:
                        w = reader.ReadSingle();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new Vector4(x, y, z, w);
            return result;
        }
    }
    #endregion Vector
}