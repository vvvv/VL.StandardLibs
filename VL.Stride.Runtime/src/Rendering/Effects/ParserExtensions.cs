using System;
using System.Linq;
using Stride.Core.Mathematics;
using System.Collections.Generic;
using Stride.Shaders.Parsing.SDSL.AST;
using System.Numerics;
using Vector2 = Stride.Core.Mathematics.Vector2;
using Vector3 = Stride.Core.Mathematics.Vector3;
using Vector4 = Stride.Core.Mathematics.Vector4;

namespace VL.Stride.Rendering
{
    public static class ParserExtensions
    {
        public static bool TryGetAttribute(this ShaderMember v, string attrName, out AnyShaderAttribute attribute)
        {
            if (v.Attributes is not null)
            {
                foreach (var a in v.Attributes)
                {
                    if (a is AnyShaderAttribute decl && decl.Name == attrName)
                    {
                        attribute = decl;
                        return true;
                    }
                }
            }

            attribute = null;
            return false;
        }

        public static string GetKeyName(this ShaderMember v, ShaderClass shader)
            => shader.Name + "." + v.Name;

        public static string ParseString(this AnyShaderAttribute attr)
        {
            return attr.Parameters.FirstOrDefault()?.GetStringValue();
        }

        public static List<string> ParseStringList(this AnyShaderAttribute attr)
        {
            return attr.Parameters
                .Select(p => p?.GetStringValue())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }

        public static List<string> ParseStringAsCommaSeparatedList(this AnyShaderAttribute attr)
        {
            return attr.Parameters
                .Select(p => p?.GetStringValue())
                .Where(s => s != null)
                .SelectMany(s => s.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct()
                .ToList();
        }

        public static bool ParseBool(this AnyShaderAttribute attr, int index = 0) => attr.Parameters.ElementAtOrDefault(index).GetBoolDefault();

        public static float ParseFloat(this AnyShaderAttribute attr, int index = 0) => attr.Parameters.ElementAtOrDefault(index).GetFloatValue();

        public static int ParseInt(this AnyShaderAttribute attr, int index = 0) => attr.Parameters.ElementAtOrDefault(index).GetIntValue();

        public static Int2 ParseInt2(this AnyShaderAttribute attr) => new Int2(attr.ParseInt(0), attr.ParseInt(1));

        public static Int3 ParseInt3(this AnyShaderAttribute attr) => new Int3(attr.ParseInt(0), attr.ParseInt(1), attr.ParseInt(2));

        public static Int4 ParseInt4(this AnyShaderAttribute attr) => new Int4(attr.ParseInt(0), attr.ParseInt(1), attr.ParseInt(2), attr.ParseInt(3));

        public static object ParseBoxed(this AnyShaderAttribute attr, Type type, object defaultVlaue = null)
        {
            if (type == typeof(float))
                return attr.Parameters.ElementAtOrDefault(0).GetFloatValue();

            if (type == typeof(Vector2))
                return new Vector2(attr.Parameters.ElementAtOrDefault(0).GetFloatValue(), attr.Parameters.ElementAtOrDefault(1).GetFloatValue());

            if (type == typeof(Vector3))
                return new Vector3(attr.Parameters.ElementAtOrDefault(0).GetFloatValue(), attr.Parameters.ElementAtOrDefault(1).GetFloatValue(), attr.Parameters.ElementAtOrDefault(2).GetFloatValue());

            if (type == typeof(Vector4))
                return new Vector4(attr.Parameters.ElementAtOrDefault(0).GetFloatValue(), attr.Parameters.ElementAtOrDefault(1).GetFloatValue(), attr.Parameters.ElementAtOrDefault(2).GetFloatValue(), attr.Parameters.ElementAtOrDefault(3).GetFloatValue());

            if (type == typeof(Color4))
                return new Color4(attr.Parameters.ElementAtOrDefault(0).GetFloatValue(), attr.Parameters.ElementAtOrDefault(1).GetFloatValue(), attr.Parameters.ElementAtOrDefault(2).GetFloatValue(), attr.Parameters.ElementAtOrDefault(3).GetFloatValue());

            if (type == typeof(bool))
                return attr.Parameters.ElementAtOrDefault(0).GetBoolDefault();

            if (type == typeof(int))
                return attr.Parameters.ElementAtOrDefault(0).GetIntValue();

            if (type == typeof(Int2))
                return new Int2(attr.Parameters.ElementAtOrDefault(0).GetIntValue(), attr.Parameters.ElementAtOrDefault(1).GetIntValue());

            if (type == typeof(Int3))
                return new Int3(attr.Parameters.ElementAtOrDefault(0).GetIntValue(), attr.Parameters.ElementAtOrDefault(1).GetIntValue(), attr.Parameters.ElementAtOrDefault(2).GetIntValue());

            if (type == typeof(Int4))
                return new Int4(attr.Parameters.ElementAtOrDefault(0).GetIntValue(), attr.Parameters.ElementAtOrDefault(1).GetIntValue(), attr.Parameters.ElementAtOrDefault(2).GetIntValue(), attr.Parameters.ElementAtOrDefault(3).GetIntValue());

            if (type == typeof(uint))
                return (uint)attr.Parameters.ElementAtOrDefault(0).GetIntValue();

            if (type == typeof(string))
                return attr.Parameters.ElementAtOrDefault(0).GetStringValue();

            return defaultVlaue ?? Activator.CreateInstance(type);
        }

        public static T GetNumberDefault<T>(this Expression e) where T : struct, INumber<T> => e is NumberLiteral<T> l ? l.Value : default;

        public static bool GetBoolDefault(this Expression e) => e is BoolLiteral l ? l.Value : default;

        public static float GetFloatValue(this Expression e) => e is FloatLiteral l ? (float)l.Value : default;
        public static int GetIntValue(this Expression e) => e is IntegerLiteral l ? (int)l.Value : default;
        public static string GetStringValue(this Expression e) => e is StringLiteral l ? l.Value : default;

        public static Vector2 GetVector2(this Expression e)
        {
            if (e is VectorLiteral v)
                return new Vector2((float)((FloatLiteral)v.Values[0]).Value, (float)((FloatLiteral)v.Values[1]).Value);
            return default;
        }
        
        public static Vector3 GetVector3(this Expression e)
        {
            if (e is VectorLiteral v)
                return new Vector3((float)((FloatLiteral)v.Values[0]).Value, (float)((FloatLiteral)v.Values[1]).Value, (float)((FloatLiteral)v.Values[2]).Value);
            return default;
        }

        public static Vector4 GetVector4(this Expression e)
        {
            if (e is VectorLiteral v)
                return new Vector4((float)((FloatLiteral)v.Values[0]).Value, (float)((FloatLiteral)v.Values[1]).Value, (float)((FloatLiteral)v.Values[2]).Value, (float)((FloatLiteral)v.Values[3]).Value);
            return default;
        }

        public static Int2 GetInt2(this Expression e)
        {
            if (e is VectorLiteral v)
                return new Int2((int)((IntegerLiteral)v.Values[0]).Value, (int)((IntegerLiteral)v.Values[1]).Value);
            return default;
        }

        public static Int3 GetInt3(this Expression e)
        {
            if (e is VectorLiteral v)
                return new Int3((int)((IntegerLiteral)v.Values[0]).Value, (int)((IntegerLiteral)v.Values[1]).Value, (int)((IntegerLiteral)v.Values[2]).Value);
            return default;
        }

        public static Int4 GetInt4(this Expression e)
        {
            if (e is VectorLiteral v)
                return new Int4((int)((IntegerLiteral)v.Values[0]).Value, (int)((IntegerLiteral)v.Values[1]).Value, (int)((IntegerLiteral)v.Values[2]).Value, (int)((IntegerLiteral)v.Values[3]).Value);
            return default;
        }
    }
}
